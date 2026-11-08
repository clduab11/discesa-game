# Discesa — status

Resumable state. Update at the end of every working session.

## Current state (2026-11-08, post-asset-work)

Slice 1 graybox + Blender ship + SimReady crate + GLTFast are implemented and verified by automated tests and a headless player run.
Visual inspection in the Editor and a human playtest are NOT yet done. The assets shipped with that caveat.

| Item | State | Evidence |
|---|---|---|
| Unity project (URP 3D) | Done | ProjectVersion.txt = 6000.6.0f1 |
| 6DoF ship controller | Done | Assets/Scripts/Discesa/ShipController.cs |
| Input abstraction | Done | IShipInput / KeyboardMouseShipInput / DebugScriptedInput |
| Enclosed arena + pillars | Done | ArenaBuilder.cs, 40x20x40 m box |
| Enemy, weapon, objective | Done | Turret.cs, Projectile.cs, GameManager.cs |
| Win / lose / restart loop | Done | GameManager.OnTurretKilled / LoseGame / Restart |
| EditMode tests | 5/5 passed | Logs/test-results-edit.xml |
| PlayMode tests | 4/4 passed | Logs/test-results-play.xml |
| Windows standalone build | Done | Build/Discesa.exe |
| Headless runtime smoke run | 15 s, no exceptions | Logs/player-smoke.log |
| Editor visual check | NOT DONE | requires a human at the Editor |
| Human playtest | NOT DONE | docs/PLAYTEST.md is a template only |
| Blender ship (authored GLB) | Done, visual UNCHECKED | Assets/Art/Models/ship_graybox_v1.glb |
| SimReady crate (1 prop) | Done, visual UNCHECKED | Assets/Art/Models/simready_crate_trial.glb |

## Workstream 1: Unity Hub add/open (done 2026-11-08)

- Structure verified: `Assets/`, `Packages/manifest.json` + `packages-lock.json`,
  `ProjectSettings/ProjectVersion.txt` (`6000.6.0f1`, matches installed Hub Editor
  `6000.6.0f1` in `C:\Program Files\Unity\Hub\Editor`). No silent upgrade.
- Hub registry: `unity projects info .` reports `discesa-game`, `6000.6.0f1`,
  changeset `f7f8ed4d1e24`, URP, VCS github; `unity projects` lists it;
  `unity projects pin discesa-game` was run. No Hub data wipe.
- User steps (fresh machine): Hub -> "Add project from disk" ->
  `C:\Users\cld-main\Desktop\github-projects\discesa-game` -> open with
  `6000.6.0f1` -> `unity test . --mode EditMode` / `--mode PlayMode` green.
- `unity --version` is the CLI version (1.0.0-beta.17), not the Editor.

## Workstream 2: screenshot evidence helper (done 2026-11-08)

- `Assets/Scripts/Discesa/Editor/ScreenshotTool.cs`: menu
  `Discesa > Capture Game View Screenshot` writes a timestamped PNG to
  `Screenshots/` (gitignored). Play mode: attach `PlayModeScreenshotKey`, press
  `Shift+F12`. Zero binaries committed.
- Human steps: open project -> Play -> `Shift+F12` a few times -> check
  `Screenshots/` -> fill `docs/PLAYTEST.md` -> do the visual checklist below.
  Gates in the table stay NOT DONE until then.

## Workstreams 3+4: Blender ship + SimReady crate (done 2026-11-08)

- Blender 5.2.1 LTS at `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`
  (not on PATH; winget has 5.2.3 — no upgrade taken). Source script:
  `Assets/Art/Source/Blender/make_ship_graybox_v1.py` (regenerable); outputs
  `Assets/Art/Models/ship_graybox_v1.glb` (30 KB, 9 nodes, 4 materials) and the
  `.blend` (107 KB). Wiring: `ShipVisualLoader` (AssetDatabase in Editor,
  Resources in builds via `Assets/Resources/Discesa/ship_graybox_v1.glb`,
  copied by `Discesa > Wire Ship + Prop Assets`). Child `ShipVisual`, colliders
  stripped, swept controller authoritative; graybox fallback if missing.
- GLTFast `com.unity.cloud.gltfast 6.17.0` in `Packages/manifest.json` (see
  PROVENANCE). `6.21.0`/`6.22.0` are unresolvable on the registry today;
  `6.17.0` resolves, imports both GLBs, tests stay green. Crate extensions
  `EXT_meshopt_compression` + `EXT_texture_webp` import without code changes.
- Crate: `hf cp hf://datasets/codepawl/simready-glb/glb/crate_white_a14.glb`
  -> `Assets/Art/Models/simready_crate_trial.glb` (335 KB), spawned by
  `SimReadyPropLoader` at `(0,-9.2,-12)`, 35° yaw, visual-only.
  KNOWN GAP: the crate's Resources copy does not appear in
  `Build/Discesa_Data/resources.assets` (the ship's does) while it imports
  fine in the Editor. Player boots clean; re-run `Discesa > Wire Ship + Prop
  Assets` then rebuild to close the gap.

## Controls

| Action | Keys |
|---|---|
| Thrust forward / reverse | I / K |
| Strafe left / right | J / L, or hold Shift + A / D |
| Lift up / down | PageUp / PageDown |
| Pitch nose up / down | W / S or Up / Down |
| Yaw left / right | A / D |
| Roll left / right | Q / E |
| Fire | Space or left mouse |
| Restart | R |

## How to run and verify

    unity test . --mode EditMode
    unity test . --mode PlayMode
    unity build . --target StandaloneWindows64 --output-path Build/Discesa.exe --allow-dirty-build
    unity run . -- -executeMethod MaterialBaker.Bake
    unity run . -- -executeMethod DiscesaAssetWire.Wire

Pressing Play in the Editor works in any scene: GameManager bootstraps itself after scene
load, and skips Unity's InitTestScene so PlayMode tests stay isolated.

## Decisions taken

1. Unity 6000.6.0f1 + C# is the primary engine. Godot remains a documented fallback; it is
   not installed and switching needs approval.
2. Custom swept kinematic controller instead of Rigidbody physics. It mirrors the
   reference's fixed-step drag-then-move-then-slide structure while staying in SI units.
3. Materials are baked assets under Assets/Resources/Discesa. A built player strips
   shaders that no asset references, so Shader.Find returned null and material creation
   threw. That failure was reproduced in the player smoke log, then fixed this way.
4. No custom tags. Objects are identified by component, so TagManager stays untouched.
5. Ship visual is a GLTFast-imported GLB child of the controller GameObject. The
   swept controller + physics asset stay the source of truth; the mesh never brings
   colliders. Graybox primitives remain the missing-asset fallback.
6. Editor-only helpers (`ScreenshotTool`, `DiscesaAssetWire`, `MaterialBaker`) live in the
   Editor assembly (`Assets/Scripts/Discesa/Editor/`). The runtime assembly must not
   reference `UnityEditor` or the Input System package — that broke compilation once
   (CS0234 in `ScreenshotTool.cs:91`, fixed by probing the Input System reflectively
   in the Editor assembly only).
7. GLTFast pinned to 6.17.0 — newer versions failed resolution on the registry.

## Physics tuning (slice 1 defaults)

mass 100 kg, maxThrust 1600 N, maxSlideThrust 1200 N, drag 0.8 /s, rotthrust 2.4 rad/s,
rollRate 2.6 rad/s, rotDrag 8 /s, maxSpeed 55 m/s.
Terminal forward speed about 20 m/s, lateral about 15 m/s.

## Open items, in order

1. Editor visual check: ship, arena, HUD, crosshair, and camera not clipping walls.
2. Human playtest: fill docs/PLAYTEST.md for comfort, readability, responsiveness.
3. Rebuild after `Discesa > Wire Ship + Prop Assets` and confirm the SimReady
   crate appears in `Build/Discesa_Data/resources.assets` (see gap above).
4. Mobile input prototype: only after the desktop loop is judged fun.

## Known gaps (deliberate)

- No banking or auto-levelling; the ship rotates exactly where you point it.
- Projectiles are primitive spheres; no muzzle flash, hit VFX, or audio.
- The turret does not lead its target and has no line-of-sight check.
- No pause, settings, or sensitivity options yet.
- Nothing has been tuned by a human yet; the numbers come from the model, not from feel.
