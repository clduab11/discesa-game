# Provenance

## Reference code (study only, nothing copied into this repo)
- Descent 1 source release `videogamepreservation/descent`, pinned commit `f6fb1c30a06725b403dfa7db4b251ecea1c3efc1`.
- License (README.TXT): © Parallax Software 1993–1998. Royalty-free perpetual license for noncommercial, royalty/revenue-free use only. Revenue-bearing use prohibited.
- Usage: behavioral study of `PHYSICS.C` (`do_physics_sim` 1/64s substeps, drag-before-move, sweep-slide), `CONTROLS.C` (`read_flying_controls` fvec/rvec/uvec composite), `OBJECT.H` (`physics_info` schema). All movement code in `Assets/Scripts/Discesa/ShipController.cs` is original rewritten code; no Parallax code, names, art, music, levels, or branding included.
- REA (MIT) not installed/used — source answered behavioral questions. BLA (MIT) not used; deferred until a working prototype (this slice now exists).

## Assets in this repo
- Unity primitives (Box, Sphere, Cylinder) instanced at runtime with URP Lit materials — no external assets in slice 1.
- HUD: UnityEngine.UI text — original.

## Future assets (HF dataset annex, rev 2)
| Dataset | License (verified 2026-11-07) | Use |
|---|---|---|
| codepawl/simready-glb | CC-BY-4.0 (credit NVIDIA) | primary 3D trial: 1–5 GLB props |
| nvidia/simready-assets | CC-BY-4.0 | provenance source, no bulk pull |
| nvidia/USDCraft-10K | Apache-2.0 | interactive-prop reference |
| scenario-labs/3d-collection | CC-BY-4.0 (credit Scenario) | stylized GLB reference |
| ccore/ThreeJS-3D-GLB-Assets | MIT | individual GLB use |
| allenai/objaverse / -xl | ODC-By + per-object | inspection only |
| Angais/pixel-art-atlas-1.4m | CC0 + Krea terms caveat | HUD icons only |
| unstonio/pixelgpt-24x24-20k | CC-BY-4.0 | HUD icons only |
| carlosuperb/lpc-* | CC-BY-SA 3.0 | reference only |
| azyg0s/pixelart-172k + -50m | MIT | MIT CPU icon generation |
| sprited/sprite-dx-data | **no license = unlicensed** | excluded from use |
| Fraser/pico-8-games | CC-BY-NC-SA 4.0 | study only |
