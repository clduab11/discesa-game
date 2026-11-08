# Discesa — status

Resumable state. Update at the end of every working session.

## Current state (2026-11-08)

Slice 1 graybox is implemented and verified by automated tests and a headless player run.
Visual inspection in the Editor and a human playtest are NOT yet done.

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
| Human playtest | NOT DONE | docs/PLAYTEST.md is empty |

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
    unity build . --target StandaloneWindows64 --output-path Build/Discesa.exe
    unity run . -- -executeMethod MaterialBaker.Bake

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

## Physics tuning (slice 1 defaults)

mass 100 kg, maxThrust 1600 N, maxSlideThrust 1200 N, drag 0.8 /s, rotthrust 2.4 rad/s,
rollRate 2.6 rad/s, rotDrag 8 /s, maxSpeed 55 m/s.
Terminal forward speed about 20 m/s, lateral about 15 m/s.

## Open items, in order

1. Editor visual check: ship, arena, HUD, crosshair, and camera not clipping walls.
2. Human playtest: fill docs/PLAYTEST.md for comfort, readability, responsiveness.
3. Blender mini-loop: one authored ship mesh, GLB export, import as prefab.
4. GLB dataset trial: one prop from codepawl/simready-glb (CC-BY-4.0, credit NVIDIA)
   through GLTFast, recorded in docs/PROVENANCE.md.
5. Mobile input prototype: only after the desktop loop is judged fun.

## Known gaps (deliberate)

- No banking or auto-levelling; the ship rotates exactly where you point it.
- Projectiles are primitive spheres; no muzzle flash, hit VFX, or audio.
- The turret does not lead its target and has no line-of-sight check.
- No pause, settings, or sensitivity options yet.
- Nothing has been tuned by a human yet; the numbers come from the model, not from feel.
