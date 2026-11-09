# Reference source — study only

The behavioural reference for this prototype is the Descent 1 source release:

- Repository: https://github.com/videogamepreservation/descent
- Pinned commit: `f6fb1c30a06725b403dfa7db4b251ecea1c3efc1`
- Licence (README.TXT): © Parallax Software 1993–1998. Royalty-free perpetual licence for
  noncommercial, royalty/revenue-free use only. Revenue-bearing use prohibited.

**Nothing from this repository is imported into version control.** No source code, art,
textures, music, level geometry, names or branding from it appears anywhere under
`Assets/`, `Packages/`, `ProjectSettings/` or `docs/`. The interaction with it is a
behavioural study only; every implementation in `Assets/Scripts/Discesa/` is original
code written for this project.

## Why there is no vendored copy

Vendoring the reference (even as a submodule) would place third-party source inside the
project's history and invite accidental copying, which the prototype's non-negotiable
constraints forbid. Instead the study checkout is opt-in and lives outside version
control at `reference/descent/` (gitignored). A second local unpacked copy,
`descent-master/`, exists for convenience and is also gitignored; neither path is read by
the build. `git status` lists neither, and both can be removed without affecting the game.

## Fetch the pinned study copy (optional, local only)

```powershell
pwsh -File tools/fetch-reference.ps1
```

The script performs a blobless clone of the repository and checks out exactly the pinned
commit into `reference/descent/`. That directory is ignored by git, so `git status` stays
clean and nothing is committed.

## Files studied (behaviour only)

| File | What was learned | Where our original implementation lives |
|---|---|---|
| `MAIN/PHYSICS.C` | 1/64 s substeps, thrust then per-substep drag, swept segment move, wall slide, end-of-frame velocity re-derived from actual movement | `ShipController.cs` |
| `MAIN/CONTROLS.C` | Thrust composed from forward/right/up axes scaled by max thrust; rotational command composed from pitch/yaw/roll | `ShipController.StepTranslation/StepRotation` |
| `MAIN/SEGMENT.H` | Mine is a set of cube segments with explicit side connections/openings | `ArenaLayout.cs`, `ArenaBuilder.cs` |
| `MAIN/RENDER.C` | First-person eye at the viewer position and orientation; optional forward eye offset; cockpit / rear / full-screen view modes | `CockpitCamera.cs`, `CameraRig.cs` |
| `MAIN/COCKPIT.ASM`, `MAIN/MCOCKPIT.C` | Cockpit is a 2D overlay with transparent holes, not a 3D interior | `CockpitHud.cs` |
| `MAIN/GAUGES.C` | Shields, energy, weapon picture/ammo, centre reticle layers | `HudModel.cs`, `CockpitHud.cs` |
| `MAIN/PLAYER.H` | Energy/shields capped at 100; weapon and ammo slots | `ShipStatus.cs`, `WeaponDefinition.cs` |
| `MAIN/WEAPON.H` | Weapon info fields: energy use, ammo use, fire wait, fire count, strength, speed, lifetime, splash/homing/persistence flags | `WeaponDefinition.cs`, `WeaponSystem.cs` |
| `MAIN/POWERUP.H` | Energy and missile pickups | `Pickup.cs` |
| `MAIN/ROBOT.H`, `MAIN/AI.C` | Robot states (rest/search/lock/fire/recover), FOV, firing wait, lead/random spread | `Turret.cs`, `Walker.cs` |
| `MAIN/PHYSICS.C:404-426` (`set_object_turnroll`) | Turn banking: `desired_bank = -fixmul(rotvel.y, TURNROLL_SCALE)`; slew rate `fixmul(ROLL_RATE, FrameTime)` toward it; stored as `phys_info.turnroll` (16.16 fixang, 65536 = one turn) | (not yet ported — next increment) |
| `MAIN/PHYSICS.C:515-546` (`do_physics_sim_rot` bank dance) | Bank is virtual: unroll by `-turnroll` before the rotational-velocity rotation, apply velocity rotation, re-roll by `+turnroll` after — base orientation never accumulates the bank | (not yet ported — next increment) |
| `MAIN/PHYSICS.C:312-402` (`do_physics_align_object`) | Segment-relative leveling: best-aligned segment side normal becomes desired up; |fvec·desiredUp| ≥ F1_0/2 skips; |delta+turnroll| ≤ DAMP_ANG (0x400) deadband; roll rate ROLL_RATE×FrameTime applied directly to orientation at end of sim; the two-face normals averaging branch does not apply to our AABB volumes | (not yet ported — next increment) |
| `MAIN/PHYSICS.C:1048-1050` | `PF_LEVELLING` gate: align runs after move/collision response in `do_physics_sim` | (not yet ported — next increment) |
| `MAIN/PHYSICS.C:469-509` | Rotational drag is `drag*5/2` per full tick (ratio already ported as `rotDrag = 2.01906835/s` earlier) | `ShipPhysics.cs` (ported) |

## Licence summary and exclusions

- The reference is not a source of assets. Nothing under `Assets/Art/` derives from it.
- Names used in this project are original: ship **SKIFF**; weapons **pulse / spread /
  heavy**; pickups **cell / missile**. No reference weapon, robot or ship names are used.
- The pinned commit is the only revision consulted. Re-verify the licence before any
  future study session; see `docs/PROVENANCE.md` for the dated verification record.
