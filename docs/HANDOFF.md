# Hand-off

Paste-ready state for a fresh agent session, and the first Unity AI prompt.
Written 2026-11-08.

---

## 1. State of the repo (verified this session)

- Repo: https://github.com/clduab11/discesa-game — **HEAD `d68b31f`**, branch `main`, pushed.
- **The working tree is ahead of HEAD: 119 uncommitted files, 46 of them under `Assets/Scripts`.**
  This is prior-session work (M0–M5 slice) that was never committed. It has **not** been compiled,
  tested or smoke-run in this session. Do not assume it is green. Do not commit it under a message
  that does not describe it; check `git status` first.
- Engine: Unity 6000.6.0f1, URP 3D, Blender 5.2.1. Project is **Input System only**
  (`activeInputHandler: 1`); legacy `Input.*` throws.
- Documented in: `AGENTS.md`, `docs/STATUS.md`, `docs/PROVENANCE.md`, `docs/PLAYTEST.md`,
  `docs/PLAN.md`, `docs/UNITY_AI_PLAN.md`, `docs/CREDIT_LEDGER.md`, `docs/REFERENCE_IMAGES.md`,
  `docs/VIRAL_WORKFLOWS.md`.
- Verified working, with evidence on disk: EditMode and PlayMode suites (see `Logs/`), a Windows
  standalone build, and a headless player run. **No human playtest, no visual inspection.**
- Asset pipeline: `tools/blender-lpm` (MIT, vendored, gates PASS on Blender 5.2.1) →
  `_work/<asset>/*.fbx` + BaseMask PNGs. Reference art would come from Venice
  (`docs/REFERENCE_IMAGES.md`); none generated yet.
- Unity AI: 1,000 credits authorised, 800 soft cap, **0 spent**, ledger at `docs/CREDIT_LEDGER.md`.

### Feature inventory on disk (names only — not verified working)

`ShipController`, `ShipPhysics`, `ShipCamera`, `CameraRig`, `CockpitCamera`, `CockpitHud`,
`ArenaBuilder`, `ArenaLayout`, `ArenaContainment`, `GameManager`, `Turret`, `Walker`,
`CoreObjective`, `WeaponSystem`, `WeaponDefinition`, `Projectile`, `ProjectilePool`, `Pickup`,
`ShipStatus`, `Vfx`, `HudModel`, `HudArt`, `PhotoDirector`, `ShipVisualLoader`,
`SimReadyPropLoader`, `DiscesaMaterials`, `LatchedShipInput`, `IShipInput`.

### The one gate that matters

`docs/PLAYTEST.md` is still empty. Nothing is "done" until a human fills it in.

---

## 2. First Unity AI prompt — copy this

**Before pasting, set:**

| Control | Value | Why |
|---|---|---|
| Model | **Unity Lite** | 13–25 credits for a plan vs 150+ on Ultra |
| Mode | **Plan** | Review + plan, no file edits, no agent loop |
| Attached files | `Assets/Scripts/Discesa/ShipController.cs`, `ArenaContainment.cs`, `ShipPhysics.cs`, `ArenaLayout.cs`, `docs/STATUS.md` | Only the files the question needs; cost scales with context |

**Prompt:**

```
You are reviewing a Unity 6000.6.0f1 URP six-degree-of-freedom flight prototype. Be specific and
sceptical; I care about failure modes, not encouragement.

Context: the ship uses a custom swept kinematic controller (not Rigidbody physics). Each 64 Hz
substep applies thrust from the ship's own axes, exponential drag, then moves with a swept BoxCast
and a wall-slide response, plus a hard clamp to an arena bounding box. Input comes through
IShipInput and this project is Input System only.

Read only these files: ShipController.cs, ArenaContainment.cs, ShipPhysics.cs, ArenaLayout.cs,
and docs/STATUS.md.

Answer three things, no more:

1. FAILURE MODES. List every way the swept move, the slide response, or the containment clamp can
   produce a wrong result: tunnelling, sticking, corner jitter, inverted normals, self-collision,
   accumulating drift, scale or unit errors, order-of-operations bugs. For each, name the exact
   line or method and the input sequence that triggers it.

2. WHAT TO MEASURE. Give me the minimum set of automated checks that would catch those failures,
   in the style of a PlayMode test: what to set up, what to assert, and the numeric tolerance you
   would use. Assume the tests already exist for terminal velocity, drag decay, rotation axes and
   no-tunnelling at max speed.

3. NEXT INCREMENT. One page maximum: the smallest change that improves flight feel on desktop
   without adding a mechanic. Rank three candidates by (player impact) divided by (risk to the
   working slice), and say which single value in ShipPhysics.cs you would tune first and to what.

Do not modify any files. Do not propose new systems, new assets, monetisation, or a different
engine. If something in the code is ambiguous, say so instead of guessing.
```

**Expected cost:** ~13–25 credits on Lite in Plan mode. Check the Dashboard after it answers and
record reserved vs charged in `docs/CREDIT_LEDGER.md`.

**Why this as the first prompt:** the swept-move and containment code is the highest-risk part of
the prototype — a subtle sign or ordering error there makes the ship clip or stick, and that is
hard to see by reading. It also costs less than a Default review (~50) and produces a test list we
can act on immediately.

---

## 3. What to do with the answer

1. Do **not** paste the whole reply back into the repo. Paste it to me.
2. I will turn the failure list into PlayMode tests, run them, and report which ones actually fail
   against the current code.
3. Only then do we change anything, and the change is one value or one method at a time.

**Reject the answer if it:** proposes generating 3D models, proposes a different engine, rewrites
the controller wholesale, or claims a test passed without showing the assertion.

---

## 4. Queued prompts (do not run until the above is handled)

| # | Task | Model/mode | Est. | Gate before running |
|---|---|---|---|---|
| 2 | Arena skybox matching the palette | Default, Agent | ~12 | First prompt answered |
| 3 | Six one-shot SFX: fire, hit, explosion, thrust, alarm, UI | Default | ~156 | `docs/PLAYTEST.md` filled — the playtest decides which sounds matter |

---

## 5. Hand-off checklist for the next agent session

- [ ] `git status` — decide what the 119 uncommitted files are before touching anything.
- [ ] `unity test . --mode EditMode` and `--mode PlayMode` — establish a baseline, do not assume.
- [ ] `unity build . --target StandaloneWindows64 --output-path Build/Discesa.exe` — confirm it still builds.
- [ ] Read `AGENTS.md` "Rules that have already cost us a session" before editing C#.
- [ ] Read `docs/PLAYTEST.md`; if empty, the next human action is a playtest, not more features.
- [ ] Update `docs/STATUS.md` at the end, including anything left unverified.
