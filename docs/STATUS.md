# Discesa — status

Resumable state, updated 2026-11-08 after kinematic remediation and reference damping tuning.

## Current state

The M0–M5 slice is implemented, tested, built and smoke-run. The cockpit, HUD, three-room
arena, turret, walker, core objective, three weapons, two pickups and the win/lose/restart
loop are in the build. The physics increment below is tested and rebuilt. Milestone screenshots
and the player smoke log belong to the earlier visual/capture pass, not this physics session.

| Item | State | Evidence |
|---|---|---|
| Unity project, URP, Input System | Done | `ProjectSettings/ProjectVersion.txt` = 6000.6.0f1 |
| 6DoF ship, swept collision, containment | Done | `ShipController.cs`, `ShipPhysics.cs`, `ArenaContainment.cs` |
| Cockpit overlay + HUD + reticle | Done | `CockpitHud.cs`, `HudArt.cs`, `HudModel.cs`, `CockpitCamera.cs` |
| Three rooms + two corridors + service alcove | Done | `ArenaLayout.cs`, `ArenaBuilder.cs` |
| Textured industrial arena | Done | procedural textures under `Assets/Art/GeneratedFallback/Textures/` |
| Turret and walker enemies | Done | `Turret.cs`, `Walker.cs` |
| Core objective + win/lose/restart | Done | `CoreObjective.cs`, `GameManager.cs` |
| Three weapons + pooled projectiles/VFX | Done | `WeaponSystem.cs`, `ProjectilePool.cs`, `Vfx.cs` |
| Two pickups with respawn | Done | `Pickup.cs` |
| Deterministic photo poses | Done | `PhotoDirector.cs` (13 poses) |
| Audio (Pilot) | Candidate prepared; Shield deferred | `Assets/Audio/SFX/Prototypes/Prepared/PulseShot_01_OneShot.wav`; not integrated, human audition pending; ShieldImpact requires human selection or another source |
| EditMode tests | 21/21 passed | `Logs/physics-editmode.xml`, 2026-11-08 22:49:58Z |
| PlayMode tests | 32/32 passed | `Logs/physics-playmode.xml`, 2026-11-08 22:50:46Z; 0 failed/skipped |
| Windows standalone build | Succeeded, exit 0 | `Build/Discesa.exe`; `Logs/build-StandaloneWindows64-1794178290128.log:18979-18980` |
| Headless-ish player smoke run | Earlier build only: 15 s, no exceptions | `Logs/player-smoke.log`; not repeated after physics changes |
| Milestone screenshots | 13 captured and reviewed | `docs/evidence/M1..M5` |
| Human playtest | NOT DONE | `docs/PLAYTEST.md` is still a blank sheet |

## Controls

| Action | Keys |
|---|---|
| Pitch | `W`/`S` or `↑`/`↓` |
| Yaw | `A`/`D` |
| Roll | `Q`/`E` |
| Thrust | `I`/`K` |
| Strafe | `J`/`L` (hold `Shift` to turn yaw into strafe) |
| Vertical | `PageUp`/`PageDown` |
| Fire | `Space` or left mouse |
| Weapon select | `1` Pulse, `2` Spread, `3` Heavy, `Tab` cycle |
| Camera | `V` toggles cockpit / beauty |
| Restart | `R` |

## Screenshot evidence

| Milestone | Files |
|---|---|
| M1 | `docs/evidence/M1/01_cockpit_hud.png`, `02_damage_state.png`, `03_beauty_ship.png` |
| M2 | `docs/evidence/M2/01_corridor.png`, `02_gallery.png`, `03_core_chamber.png` |
| M3 | `docs/evidence/M3/01_turret_fire.png`, `02_projectile_trail.png`, `03_pickup_cell.png`, `04_walker_burst.png` |
| M5 | `docs/evidence/M5/01_win.png`, `02_lose.png`, `03_restart.png` |

The working copies live in gitignored `Screenshots/` and are produced by
`PhotoDirector` poses at 1280×720 via `unity command capture game_view --source screen`.

## Verification commands

With the Editor open, use the Pipeline commands (the `unity test/build/run` CLI is blocked
while an Editor holds the project):

```powershell
unity command test run --mode EditMode --async-tests
unity command test status
unity command test run --mode PlayMode --async-tests
unity command test status
unity command build run --target StandaloneWindows64 --output-path Build/Discesa.exe --confirm
unity command build status
```

Earlier visual-pass build: `Succeeded`, 0 errors, 510 warnings, 313 s.
Latest physics build: `Build Finished, Result: Success`, process exit 0, no C# compiler errors
found in its log. Licensing-client validation/entitlement diagnostics were emitted despite
successful completion; this is not a warning-free-log claim.

Earlier visual-pass smoke run (15 s, windowed 1280×720):

```text
[Discesa] Ready: ship at (-32.00, 0.00, 0.00), core at (31.00, 2.00, 8.00) (150 hp),
turret at (26.50, 0.40, 5.80) (60 hp), walker at (2.00, -5.30, 8.00) (80 hp),
rooms 3 corridors 2 bays 1, camera CockpitCamera
```

No exceptions in the log. Two benign warnings: the URP depth-of-field shaders are stripped
from the player (the volume asks for DOF), and the shadow atlas reduces punctual shadow
resolution to fit six maps. Both are harmless at this scope; disabling DOF in the URP asset
would silence the first.

## Open items

1. **Human playtest** — the only unverified quality gate. Use `docs/PLAYTEST.md`.
2. **Build warnings** — historical visual-pass count: 510. Latest build succeeded, but warning
   and licensing-client diagnostic cleanup is not complete; do not infer a current warning count.
3. **Enemy readability** — turret and walker are still primitive box assemblies. The walker
   reads at close range; at distance it is a tan block.
4. **Audio** — PulseShot audition candidate prepared under `Assets/Audio/SFX/Prototypes/Prepared/`; ShieldImpact needs human timestamp selection or a different source. Not integrated; human audition pending.
5. **Menus/settings/pause** — none; `R` restarts.
6. **Unity AI credits** — actual reservations and charges for the two-clip pilot are unknown until Dashboard confirmation. 1,000 authorised, 800 soft cap; no further generation jobs submitted during local preparation. AI texture generation remains an option, not a dependency.
7. **M0 baseline commit** — not made; the tree is dirty by design until the human gate.
8. **Reference banking/leveling** — constants and segment-relative behavior are extracted below,
   but automatic banking/leveling is deliberately not enabled in this increment.
9. **Query allocation/performance** — `OverlapBox`/`BoxCastAll` allocate arrays and transforms
   synchronize per substep. Profile before replacing these with bounded reusable buffers;
   buffer saturation must never drop a blocking collider silently.

## Kinematic increment (2026-11-08)

Actual script paths are `Assets/Scripts/Discesa/`, not `Assets/Scripts/`.
Existing dirty/untracked work was preserved. No reference code or new assets were copied,
no Blender proxy was required, no Unity AI credits were spent, and the human playtest remains
unperformed. No new player smoke run or visual inspection is claimed.

### Failure modes fixed

- Initial overlap: recover real minimum-separation normals with `ComputePenetration` before
  sweeping. Never treat the synthetic zero-distance BoxCast normal (`-direction`) as a wall
  normal. The query-only trigger collider is enabled synchronously for penetration queries,
  then disabled before physics simulation. Rotation-only overlaps also recover.
- Swept wall slide: retry up to eight contacts, consume only remaining displacement, and remove
  only inward normal momentum. Removed the artificial 0.9 impact-speed multiplier; separating
  and crease-tangent momentum survive. Contact-cone projection prevents sequential plane clips
  from re-entering earlier walls in 30/45-degree crevices; a three-plane corner can stop safely.
- Broadphase consistency: synchronize transform-driven arena/teleport changes before queries,
  preventing stale collider poses from becoming false distance-zero sweep constraints.
- Containment ordering: recover expired escapes before input/movement can hide them; validate
  the resulting position with zero extra elapsed time. Grace/dwell timers advance once per
  substep. Only continuously safe, rotated-hull-clear positions are cached; exterior tolerance,
  interrupted dwell, and wall intersections cannot poison the checkpoint. Reset clears timers.

### Reference extraction and tuning

Only local `descent-master/MAIN/PHYSICS.C`, `FVI.C`, and `COLLIDE.C` were inspected. Relevant
ranges: physics 269-699 and 700-1049; face/edge intersection 472-638; wall dispatcher 2037-2058.
The implementations are original C# behavioral adaptations, not copied C.

| Reference | Conversion / use |
|---|---|
| `FT = F1_0/64`, fix `1024` (`0x400`) | `0.015625f` s; retain 64 Hz integration |
| Per-tick add thrust/mass, then retain `1-d` | SI impulse is `(thrust/mass)*FT`; reference per-tick thrust scales by 64 when expressed as force with the same mass |
| Existing project linear rate `0.8/s` | `d64 = 1-exp(-0.8/64) = 0.0124221995`; not a claim about the original ship table |
| Rotational drag expression `(drag*5)/2` | Ratio `2.5f` (16.16 equivalent `163840`, `0x28000`); angular retention `0.9689445012` per tick |
| Converted angular exponential pole | `rotDrag = -64*ln(0.9689445012) = 2.01906835f /s`, replacing default `8f`; manual/custom overrides remain intact |
| `ROLL_RATE=0x2000` | `0.125` turns/s = `0.785398163` rad/s; `0.0122718463` rad per tick; extracted only |
| `DAMP_ANG=0x400` | `5.625` degrees = `0.0981747704` rad deadband; extracted only |
| `TURNROLL_SCALE=0x4ec4/2=10082` | `0.153839111`; desired bank is negative yaw rate times this scale; extracted only |
| Alignment guard `F1_0/2=32768` | `0.5f`; nearest segment-side up normal, not global up; extracted only |

`ShipPhysics.cs` records original constants and float conversions next to the tuning. The
highest-leverage implemented feel change is the reference rotational damping pole at the
existing SI linear tuning, not forced auto-leveling or a full integer-accurate flight port.
Source integer truncation is not reproduced. Thrust and turn-rate targets are unchanged.
Exact discrete steady-state speeds are 19.87526 m/s forward and 14.90645 m/s lateral, using
`vInf = (thrust/mass)*FT/(exp(drag*FT)-1)` rather than the continuous approximation `a/drag`.

### Recorded verification

The pre-existing kinematic regression fixture was preserved and six new tests were added in
one batched edit before production edits. Initial red run: 32 executed, 19 passed, 13 failed.
Two bounded remediation cycles isolated the enabled-query-shape requirement; mathematical
analysis of stale broadphase poses then motivated explicit transform synchronization. The
following full tuning run passed 32/32, and the final independent reports also passed:

```powershell
unity test . --mode EditMode --output Logs/physics-editmode.xml
unity test . --mode PlayMode --output Logs/physics-playmode.xml
unity build . --target StandaloneWindows64 --output-path Build/Discesa.exe --json
git diff --check
```

- EditMode: 21 passed, 0 failed, 0 skipped, 0.232171 s.
- PlayMode: 32 passed, 0 failed, 0 skipped, 7.5721849 s. Includes all 16 kinematic/tuning
  regressions and all original arena, combat, pickups, camera, photo-loop and ship tests.
- Windows player build: successful, exit 0. Recorded log success at lines 18979-18980 in
  `Logs/build-StandaloneWindows64-1794178290128.log`; provenance at `Build/Discesa.provenance.json`.
- `git diff --check`: exit 0; Git emitted line-ending conversion warnings, not whitespace errors.

### Session budget

Total Tool Calls Used: 65 / 110

| Phase | Calls | Cap |
|---|---:|---:|
| Initial context/path inspection | 12 | 12 |
| Step 1: tests/remediation, including scoped C inspection after path correction | 33 | 35 |
| Step 2: damping extraction/conversion, port, tests and queue-budget review | 9 | 25 |
| Optional Blender | 0 | 15 |
| Step 3: final verification, status and completion bookkeeping | 11 | 15 |

## 2026-11-08 session 2 (bot gate start; flight options not started)

**Increment goal:** (1) deterministic bot-playthrough PlayMode gate, (2) configurable
turn-banking + segment-relative leveling. **Both are INCOMPLETE this session.**

### What exists now (new file only, no runtime code touched)

`Assets/Tests/PlayMode/BotPlaythroughTests.cs` — GameManager-built world, `DebugScriptedInput`-style
`IShipInput` bot seam, ship stepped explicitly at `fixedDt = 1/64` with `ship.enabled = false`
(no double simulation), per-step asserts: layout union containment, `RespawnCount == 0`, no NaN.
Runs the spawn route (derived from ArenaLayout/ArenaBuilder door spans: EntryEast door x=-17
z -3..3 y -6..0, GalleryWest x=-6 z -2..3, GalleryToCorridorB x=10 z 5..11, CoreWest x=18 z 5..11)
then `weapons.TryFire()` duels the turret and kills the core, asserts `IsWin`, then
`gm.Restart()` and asserts full state restoration (ship pose/velocity, shields/energy/missiles,
containment counters, core/turret/walker health, pool drained, firing works again). Control case:
same route, zero weapon use, dwells, and asserts `IsWin` stays false and nothing is damaged.
Nice detail confirmed working in runs: legs 0–3 flew cleanly; walker capsule (top y≈-3.4) forced
gallery legs to y≈-2 hull-clear.

### Recorded results (fresh, in-Editor Pipeline, Editor PID 41972)

- PlayMode: 34 total / 32 passed / **2 failed** — both new bot tests still fail
  (`navigate leg 2 z drift +7.2`, and a second variant where the run burned its step budget
  with the ship parked at spawn, `v=(0,0,0)` — containment early-return is the lead suspect;
  not yet diagnosed). Prior 32 PlayMode tests all still pass.
- EditMode: 21/21 passed (fresh run this session; runtime code unchanged since).
- Build: **not run this session** (bot gate not green; budget exhausted on guidance debugging).
- Guidance variants already tried and retired: naive point-seek (orbits waypoints), proportional
  brake gate (ping-pong), single-threshold atan2 seam hysteresis (flip-flops at ±180°), nose-
  pitched pursuit (parks nose-down, unrecoverable). Current pilot: level-nose legs, vertical-
  thrust altitude servo, yaw-only pure-pursuit with two-threshold (2.9/2.4 rad) seam hysteresis,
  velocity-servo combat hold — still failing; debug with per-leg `Debug.Log` of pose/commands,
  and inspect whether `ShipController.Step`'s containment entry-tick is returning early.

### Harness pitfalls (cost real calls — reuse)

- `Time.captureFramerate = 64` inside a PlayMode test trips Unity 6's runner
  "Too many instant steps in test execution mode" and Desyncs the runner (empty `total:0`
  submissions until `unity command script recompile` + resubmit recovers it; `test cancel`
  alone is unreliable).
- After editing a test file, immediately-submitted async runs are silently swallowed:
  script-recompile first, then `test run`, then poll `test status`.
- `GameManager.Awake` booby-traps duplicate Instances; `Object.Destroy` defers a frame and
  the next test's SetUp NREs — this suite uses `DestroyImmediate` in TearDown.

### Tool calls this session

| Phase | Calls |
|---|---:|
| Reads/context (docs, code, de-scoped reference study) | 26 |
| Bot-gate authoring + 6 guidance/debug iterations (run+poll cycles incl. runner-wedge recovery) | 49 |
| Verification + handoff (EditMode baseline, docs, git) | 10 |
| **Total** | **~85** |

Flight options (banking/leveling) were NOT implemented. Reference constants are already pinned
in `ShipPhysics.cs` comments and STATUS "Turn banking / segment-relative leveling" above;
do the TDD cycle there in the next increment: PlayMode `ReferenceFlightBehaviorTests.cs`
(red) covering default-off equivalence, bank sign (`desired_bank = -rotvel.y*10082/65536`,
right wing down for a right turn), ROLL_RATE slew bound (0.785398163 rad/s),
DAMP_ANG deadband (0.0981747704 rad), the |fvec·up| < 0.5 align guard (PHYSICS.C:375),
manual-roll precedence, degenerate orientation, volume transitions, collision safety;
then implement the unrotate/rotate/re-roll dance (PHYSICS.C:515-546) in
`ShipController.StepRotation` and `do_physics_align_object` semantics (PHYSICS.C:312-402,
call site 1048-1050) as a post-translation, pre-final-containment-tick roll.

Count includes unsuccessful path reads, tooling setup and the parallel-dispatch wrapper as
well as its three child calls. Remaining work is handed off as six bounded prompts in the
session response. Credit envelopes reuse the existing 350-credit Assistant bucket; the
653-credit plan and 347-credit total reserve are unchanged. Kilo model usage is not billed
against Unity AI credits; Dashboard reservations/charges require Chris and the credit ledger.

## History

The earlier graybox slice (one arena, one turret, one weapon, 5 EditMode / 4 PlayMode tests)
is preserved in git history (`56e8c5d` and `f6f3196`). This document describes the expanded
M0–M5 slice that replaced it.
