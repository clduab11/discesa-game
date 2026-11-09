using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// End-to-end bot-playthrough gate. The bot drives the real game: the GameManager-built
/// arena, the ShipController, the real corridor openings (ArenaBuilder colliders + door
/// lintels), the real weapon/projectile/damage path, the real win transition and Restart.
///
/// Harness notes (all seams are the ship's existing seams, kept minimal; documented in
/// docs/STATUS.md):
/// - Input seam: the existing IShipInput contract applied through <see cref="ShipController.input"/>.
///   The bot's pilot below is a deterministic planner driving that seam (the same seam the
///   hardware KeyboardMouseShipInput and the replay-only DebugScriptedInput use).
/// - Clock seam: the ship is advanced exactly one 64 Hz substep (fixedDt = 1/64) per yielded
///   frame, while turret/projectile/cooldown components run on the editor's natural frame
///   clock via their own Update calls (the same convention the existing CombatTests use).
///   Time.captureFramerate was deliberately rejected: in Unity 6's play-mode test harness it
///   trips the runner's "too many instant steps" guard. Enemy pacing is therefore frame-rate
///   dependent within generous budgets; the ship's own integration stays exactly 64 Hz.
///   ShipController.FixedUpdate is disabled (enabled = false), so the game is simulated once.
///   No double simulation.
/// - Fire latch seam: GameManager.Update reads the hardware Input System, which cannot be
///   faked here without devices. The bot calls WeaponSystem.TryFire, so the real
///   cooldown/energy/pool/spawn/spherecast/damage path is exercised. No direct Damage calls,
///   no win flags, no teleports, no wall edits, no enemy weakening.
/// - Random is seeded so the turret's spread/lead behaviour is reproducible.
/// - Waypoints are derived from ArenaLayout/ArenaBuilder spans: the EntryToCorridorA door
///   (x=-17, z in -3..3, lintel above y=0), CorridorA (z in -3..3), the GalleryWest door
///   (x=-6, z -2..3), the GalleryToCorridorB door (x=10, z 5..11), CorridorB (z 5..11) and
///   the CoreChamber entry (x=18, z 5..11). Gallery legs stay above the walker's capsule.
/// - Navigation is pure-pursuit on the leg line (aim at a point on the leg line ahead of
///   the nearest point), which cannot orbit a waypoint; combat phases servo the hold
///   position and aim the nose independently.
/// </summary>
public class BotPlaythroughTests
{
    const float FixedDt = 1f / 64f; // 0.015625 s per substep
    const int StepBudget = 2400;    // bounded total: 37.5 simulated seconds of ship steps
    const float Arrive = 1.35f;

    GameManager _game;
    ShipController _ship;
    WeaponSystem _weapons;
    BotInput _bot;
    int _steps;

    [SetUp]
    public void SetUp()
    {
        Random.InitState(20261108);

        var go = new GameObject("BotPlaythrough world");
        _game = go.AddComponent<GameManager>();
        _ship = _game.ship;
        _weapons = _game.weapons;
        _bot = new BotInput();
        _pilot = new BotPilot();
        _ship.SetInput(_bot);
        _ship.enabled = false; // the harness owns the 64 Hz steps; FixedUpdate must not double-step

        Assert.That(_ship.fixedDt, Is.EqualTo(FixedDt).Within(1e-7f), "64 Hz integration contract");
        Assert.That(_game.IsWin || _game.IsLose, Is.False, "fresh game must start undecided");
        Assert.That(_game.core.Hp, Is.EqualTo(_game.core.MaxHp), "core starts at full health");
        Assert.That(_game.containment.RespawnCount, Is.Zero, "no respawn at spawn");
        Assert.That(_game.layout.Contains(_ship.transform.position), Is.True, "spawn must be inside the arena");
    }

    [TearDown]
    public void TearDown()
    {
        // Immediate: Instance teardown must complete before the next test's GameManager
        // Awake runs (Object.Destroy defers a frame and would poison the next fixture).
        if (_game != null) Object.DestroyImmediate(_game.gameObject);
        Vfx.Clear();
    }

    /// <summary>Deterministic scripted input through the ship's real IShipInput seam.</summary>
    sealed class BotInput : IShipInput
    {
        public ShipInputState State;
        public ShipInputState Sample(float dt) => State;
    }

    // Spawn-facing route in leg order; each leg flies the line between consecutive points
    // with pure-pursuit steering. Altitudes keep the hull clear of lintels (door openings
    // span y -6..0) and of the walker's capsule (top -3.4).
    static readonly Vector3[] Route =
    {
        new Vector3(-27f, -2.6f, 0f),   // descend in the Entry Hall
        new Vector3(-20.5f, -2.6f, 0f), // line up with the EntryToCorridorA doorway
        new Vector3(-11.5f, -2.6f, 0f), // through the doorway and along Corridor A
        new Vector3(-4f, -2f, 0f),      // out of CorridorA into the gallery (west door z -2..3)
        new Vector3(2f, -2f, 4.5f),     // diagonal across the gallery, above the walker
        new Vector3(8f, -2f, 8f),       // line up with the GalleryToCorridorB doorway
        new Vector3(13f, -2.3f, 8f),    // Corridor B
        new Vector3(19.5f, -1f, 8f),    // Core Chamber hold point (duel + core shot position)
    };

    static float ClampCmd(float v, float dead)
    {
        if (Mathf.Abs(v) < dead) return 0f;
        return Mathf.Clamp(v, -1f, 1f);
    }

    /// <summary>
    /// Deterministic guidance pilot. Navigation is attitude-flat: the nose stays level, legs
    /// advance with forward thrust, altitude changes via a vertical-thrust servo, and heading
    /// changes via a yaw-only pure-pursuit whose turn sign is hysteresis-thresholded across
    /// the atan2 backward seam so a dead-behind aim unwinds in one continuous half-turn
    /// (engage above 2.9 rad, release below 2.4 rad; a single threshold would flip-flop
    /// forever at the seam). Combat phases aim with small-angle yaw/pitch servos on a
    /// velocity-servo hold point.
    /// </summary>
    sealed class BotPilot
    {
        float _turnSign; // held spin direction while unwinding a dead-behind aim
        bool _signHeld;

        /// <summary>Wrap-safe yaw command toward the aim point (level nose).</summary>
        public float YawCmd(ShipController ship, Vector3 aimPoint)
        {
            Vector3 rel = ship.transform.InverseTransformDirection(aimPoint - ship.transform.position);
            float horiz = Mathf.Sqrt(rel.x * rel.x + rel.z * rel.z);
            if (horiz < 1e-4f) { _signHeld = false; _turnSign = 0f; return 0f; }

            float yawErr = Mathf.Atan2(rel.x, rel.z);
            if (Mathf.Abs(yawErr) > 2.4f && _signHeld) return _turnSign;
            if (Mathf.Abs(yawErr) > 2.9f)
            {
                if (!_signHeld) { _turnSign = yawErr > 0f ? 1f : -1f; _signHeld = true; }
                return _turnSign;
            }
            _signHeld = false; _turnSign = 0f;
            return ClampCmd(yawErr * 1.7f, 0.03f);
        }

        /// <summary>Small-angle yaw/pitch commands; used for aiming from a near-aligned hold.</summary>
        public Vector2 AimCmd(ShipController ship, Vector3 aimPoint)
        {
            Vector3 rel = ship.transform.InverseTransformDirection(aimPoint - ship.transform.position);
            float horiz = Mathf.Sqrt(rel.x * rel.x + rel.z * rel.z);
            float yawErr = horiz < 1e-4f ? 0f : Mathf.Atan2(rel.x, rel.z);
            float pitchErr = horiz < 1e-4f ? 0f : Mathf.Atan2(rel.y, horiz);
            return new Vector2(ClampCmd(yawErr * 1.7f, 0.02f), ClampCmd(pitchErr * 1.7f, 0.02f));
        }

        /// <summary>Clear held turn state between legs.</summary>
        public void ResetTurnState() { _signHeld = false; _turnSign = 0f; }

        /// <summary>
        /// Pure-pursuit navigation along one leg with a level nose: yaw follows the leg line,
        /// altitude is a vertical-thrust servo, forward speed is a servo with a brake guard.
        /// </summary>
        public ShipInputState SeekLeg(ShipController ship, Vector3 legStart, Vector3 target, int phase)
        {
            Vector3 pos = ship.transform.position;
            Vector3 legV = target - legStart;
            float legLen = Mathf.Max(legV.magnitude, 1e-4f);
            Vector3 dir = legV / legLen;

            float t = Vector3.Dot(pos - legStart, dir) / legLen; // unclamped lane projection
            Vector3 aim = t >= 0.85f ? target : legStart + dir * Mathf.Min(t + 2.2f, legLen);
            float yaw = YawCmd(ship, aim);

            // Altitude servo with vertical slide thrust; the nose is never pitched.
            float vyErr = (target.y - pos.y) * 1.4f - ship.velocity.y;
            float vertical = ClampCmd(vyErr * 0.4f, 0.03f);

            // Forward speed servo toward the endpoint; brake when closure speed would overshoot.
            float dist = Vector3.Distance(pos, target);
            float thrust = 0f;
            if (dist > Arrive)
            {
                float along = Vector3.Dot(ship.velocity, dir);
                float speedTarget = Mathf.Min(14f, 5.5f * Mathf.Sqrt(Mathf.Max(dist - 1f, 0.6f)));
                thrust = ClampCmd((speedTarget - along) * 0.4f, 0.02f);

                // Do not drive while the aim sits behind (cos ~107° ≈ -0.29): a hard turn
                // stops the engine and waits for the yaw servo to unwind.
                Vector3 aimRel = ship.transform.InverseTransformDirection(aim - pos);
                float aHoriz = Mathf.Sqrt(aimRel.x * aimRel.x + aimRel.z * aimRel.z);
                if (aHoriz > 1e-3f && aimRel.z < -0.29f * aHoriz)
                    thrust = Mathf.Min(thrust, 0f);
            }

            return new ShipInputState(0f, yaw, 0f, thrust, 0f, vertical, false);
        }

        /// <summary>
        /// Independent hold-and-aim servos: the position servo drives the hold point while the
        /// attitude servo aims the nose. Used for the turret duel and the core shot.
        /// </summary>
        public ShipInputState HoldAndAim(ShipController ship, Vector3 holdPos, Vector3 aimPoint,
                                         bool weave, int phase, bool wantFire)
        {
            Vector2 cmd = AimCmd(ship, aimPoint);
            float yawErrRaw = AimYawError(ship, aimPoint);
            float pitchErrRaw = AimPitchError(ship, aimPoint);

            Vector3 vErr = (holdPos - ship.transform.position) * 1.2f - ship.velocity;
            if (weave) vErr += Vector3.up * Mathf.Sin(phase * 0.19f) * 2.5f;
            Vector3 local = ship.transform.InverseTransformDirection(vErr);
            float strafe = ClampCmd(local.x * 0.25f, 0.05f);
            float vertical = ClampCmd(local.y * 0.25f, 0.05f);
            float thrust = ClampCmd(local.z * 0.25f, 0.05f);

            bool fire = wantFire && Mathf.Abs(yawErrRaw) < 0.05f && Mathf.Abs(pitchErrRaw) < 0.05f;
            return new ShipInputState(cmd.y, cmd.x, 0f, thrust, strafe, vertical, fire);
        }

        float AimYawError(ShipController ship, Vector3 aimPoint)
        {
            Vector3 rel = ship.transform.InverseTransformDirection(aimPoint - ship.transform.position);
            float horiz = Mathf.Sqrt(rel.x * rel.x + rel.z * rel.z);
            return horiz < 1e-4f ? 0f : Mathf.Atan2(rel.x, rel.z);
        }

        float AimPitchError(ShipController ship, Vector3 aimPoint)
        {
            Vector3 rel = ship.transform.InverseTransformDirection(aimPoint - ship.transform.position);
            float horiz = Mathf.Sqrt(rel.x * rel.x + rel.z * rel.z);
            return horiz < 1e-4f ? 0f : Mathf.Atan2(rel.y, horiz);
        }
    }

    BotPilot _pilot;

    void StepOnce(ShipInputState state, string context)
    {
        Assert.That(_steps, Is.LessThan(StepBudget), $"step budget blown in {context} at {_ship.transform.position} v={_ship.velocity}");
        _steps++;
        _bot.State = state;
        _ship.Step(state, FixedDt);
        Vector3 p = _ship.transform.position;
        Assert.That(_game.layout.Contains(p), Is.True, $"{context}: ship centre left the arena union at {p}");
        Assert.That(_game.containment.RespawnCount, Is.Zero, $"{context}: containment respawned (wall escape)");
        Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z), Is.False, $"{context}: position degraded to NaN");
    }

    IEnumerator NavigateLeg(int index, string what)
    {
        Vector3 start = Route[Mathf.Max(index - 1, 0)];
        Vector3 target = Route[index];
        _pilot.ResetTurnState();
        // No per-leg time budget: pursuit transients (turn orbits) are legitimate flight;
        // the global StepBudget inside StepOnce bounds the whole run.
        while (Vector3.Distance(_ship.transform.position, target) > Arrive)
        {
            StepOnce(_pilot.SeekLeg(_ship, start, target, _steps), $"navigate {what}");
            yield return null;
        }
        Vector3 end = _ship.transform.position;
        Assert.That(Vector3.Distance(start, end), Is.LessThan(Vector3.Distance(start, target) + 2.5f),
            $"navigate {what} ended farther from its start than the leg length (teleport or slide abuse)");
    }

    IEnumerator DuelGuard()
    {
        // Turret duel from the chamber hold point: aim at the guard's collider centre,
        // servo the hold position with a weave, and fire the real pulse path until the
        // guard dies through projectile damage.
        Vector3 hold = Route[Route.Length - 1];
        Vector3 guardAim = new Vector3(26.5f, 1.1f, 5.8f);
        float guardHpStart = _game.turret.MaxHp;
        bool sawDamage = false;
        while (_game.turret.Hp > 0f)
        {
            StepOnce(_pilot.HoldAndAim(_ship, hold, guardAim, true, _steps, true), "guard duel");
            _weapons.TryFire(); // cooldown/energy self-gate; real pool + projectile + damage path
            sawDamage = sawDamage || _game.turret.Hp < guardHpStart;
            yield return null;
        }
        Assert.That(sawDamage, Is.True, "turret was never damaged through the projectile path");
        Assert.That(_game.turret.IsDead, Is.True, "guard must fall through weapon damage");
        Assert.That(_game.turret.enabled, Is.False, "destroyed guard must stop its AI");

        // Core kill at the same hold: aim at the core, pulse until its 150 hp are spent.
        Vector3 coreAim = new Vector3(31f, 1.1f, 8f);
        bool coreSawDamage = false;
        while (!_game.core.IsDestroyed)
        {
            StepOnce(_pilot.HoldAndAim(_ship, hold, coreAim, true, _steps, true), "core kill");
            _weapons.TryFire();
            coreSawDamage = coreSawDamage || _game.core.Hp < _game.core.MaxHp;
            yield return null;
        }
        Assert.That(coreSawDamage, Is.True, "core was never damaged through the projectile path");
        Assert.That(_game.core.Hp, Is.EqualTo(0f), "core must reach zero through weapon damage");
        Assert.That(_game.IsWin, Is.True, "destroying the core must actually win the game");
        Assert.That(_game.containment.RespawnCount, Is.Zero, "no respawn anywhere during the run");
    }

    IEnumerator VerifyRestartRestores()
    {
        _game.Restart();
        yield return null;

        Assert.That(_game.IsWin, Is.False, "restart cleared the win");
        Assert.That(_game.IsLose, Is.False);
        Assert.That(_ship.transform.position, Is.EqualTo(_game.SpawnPosition).Within(1e-4), "ship back at spawn");
        Assert.That(Quaternion.Angle(_ship.transform.rotation, _game.SpawnRotation), Is.LessThan(0.5f), "ship back at spawn facing");
        Assert.That(Vector3.SqrMagnitude(_ship.velocity), Is.LessThan(1e-8f), "velocity zeroed");
        Assert.That(Vector3.SqrMagnitude(_ship.angularVelocity), Is.LessThan(1e-8f), "angular velocity zeroed");
        Assert.That(_ship.IsDead, Is.False, "ship alive after restart");
        Assert.That(_game.status.Shield, Is.EqualTo(100f), "shields reset");
        Assert.That(_game.status.Energy, Is.EqualTo(100f), "energy reset");
        Assert.That(_game.status.Missiles, Is.EqualTo(6), "missile rack reset");
        Assert.That(_game.status.WeaponIndex, Is.Zero, "weapon slot 1 selected");
        Assert.That(_game.containment.RespawnCount, Is.Zero, "containment timer reset");
        Assert.That(_game.core.Hp, Is.EqualTo(_game.core.MaxHp), "core restored");
        Assert.That(_game.core.IsDestroyed, Is.False, "core restored");
        Assert.That(_game.turret.Hp, Is.EqualTo(_game.turret.MaxHp), "guard restored");
        Assert.That(_game.turret.enabled, Is.True, "guard AI restored");
        Assert.That(_game.walker.Hp, Is.EqualTo(_game.walker.MaxHp), "walker restored");
        Assert.That(_game.projectiles.ActiveCount, Is.Zero, "in-flight bolts released");

        // Restored gameplay is functional again: the pulse path fires a fresh bolt.
        for (int i = 0; i < 16; i++) yield return null; // 0.25 s: pulse cooldown run down
        bool fired = _weapons.TryFire();
        Assert.That(fired, Is.True, "post-restart weapon system must still be usable");
        Assert.That(_game.projectiles.ActiveCount, Is.EqualTo(1), "post-restart bolt spawned");
    }

    [UnityTest]
    public IEnumerator BotFlythrough_RouteDuelCore_RealWin_ThenRestartRestores()
    {
        Assert.That(_ship.transform.position.x, Is.EqualTo(-32f).Within(1e-4f),
            "bot starts at the normal spawn, not a test shortcut");

        for (int leg = 0; leg < Route.Length; leg++)
            yield return NavigateLeg(leg, $"leg {leg} ({Route[leg]})");

        yield return DuelGuard();
        yield return VerifyRestartRestores();

        Assert.That(_steps, Is.LessThan(StepBudget), "bounded step budget respected");
        Assert.That(_game.projectiles.SpawnCount, Is.GreaterThan(0), "run fired real bolts");
    }

    /// <summary>
    /// Control case: the exact route with no weapon use at all. Proves the win assertion is
    /// not satisfied by waiting, by the route itself, or by a harness shortcut — only real
    /// projectile damage on the core can win.
    /// </summary>
    [UnityTest]
    public IEnumerator BotFlythrough_NoFire_NeverWins()
    {
        for (int leg = 0; leg < Route.Length; leg++)
            yield return NavigateLeg(leg, $"control leg {leg} ({Route[leg]})");

        for (int i = 0; i < 96; i++) // 1.5 s dwell: proves waiting never wins; weave dodges incoming fire
        {
            StepOnce(new ShipInputState(0f, 0f, 0f, 0f, 0f, Mathf.Sin(i * 0.19f) * 0.65f, false), "control dwell");
            yield return null;
        }

        Assert.That(_steps, Is.LessThan(StepBudget));
        Assert.That(_game.IsWin, Is.False, "flying the route must never win");
        Assert.That(_game.core.Hp, Is.EqualTo(_game.core.MaxHp), "core untouched without weapon use");
        Assert.That(_game.core.IsDestroyed, Is.False);
        Assert.That(_game.turret.Hp, Is.EqualTo(_game.turret.MaxHp), "guard untouched without weapon use");
        Assert.That(_game.turret.enabled, Is.True);
        Assert.That(_game.walker.Hp, Is.EqualTo(_game.walker.MaxHp), "walker untouched without weapon use");
        Assert.That(_game.containment.RespawnCount, Is.Zero, "no wall escape in the control run");
        Assert.That(_ship.IsDead, Is.False, "bot survived enemy fire (AI not weakened)");
    }
}
