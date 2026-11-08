# Provenance

## Reference code (study only, nothing copied into this repo)
- Descent 1 source release `videogamepreservation/descent`, pinned commit `f6fb1c30a06725b403dfa7db4b251ecea1c3efc1`.
- License (README.TXT): © Parallax Software 1993–1998. Royalty-free perpetual license for noncommercial, royalty/revenue-free use only. Revenue-bearing use prohibited.
- Usage: behavioral study of `PHYSICS.C` (`do_physics_sim` 1/64s substeps, drag-before-move, sweep-slide), `CONTROLS.C` (`read_flying_controls` fvec/rvec/uvec composite), `OBJECT.H` (`physics_info` schema). All movement code in `Assets/Scripts/Discesa/ShipController.cs` is original rewritten code; no Parallax code, names, art, music, levels, or branding included.
- REA (MIT) not installed/used — source answered behavioral questions. BLA (MIT) not used; deferred until a working prototype (this slice now exists).

## Assets in this repo
- Unity primitives (Box, Sphere, Cylinder) instanced at runtime with URP Lit materials — slice 1 baseline, still the fallback.
- HUD: UnityEngine.UI text — original.
- **Original authored ship (2026-11-08)**: `ship_graybox_v1.glb` + source script `Assets/Art/Source/Blender/make_ship_graybox_v1.py` (+ `.blend`). Generated with Blender 5.2.1 LTS (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe --background --factory-startup --python ...`). Original low-poly design (hull, nose, canopy, wings, engine, glow strip, twin guns) in the ~0.7×0.45×1.3 m envelope. No external reference art. License: original, this repo. Attribution: none required.
- **SimReady trial prop (2026-11-08)**: `simready_crate_trial.glb` (335,036 B), file `glb/crate_white_a14.glb` from HF dataset `codepawl/simready-glb`, https://huggingface.co/datasets/codepawl/simready-glb . License **CC-BY-4.0**. Attribution required: "Models by NVIDIA SimReady, licensed CC BY 4.0. This dataset converts and compresses them. Credited to NVIDIA as original author; source dataset: https://huggingface.co/datasets/codepawl/simready-glb". What was used: single white crate, mesh patch `EXT_meshopt_compression` + `EXT_texture_webp`. Local paths: `Assets/Art/Models/simready_crate_trial.glb` (source) and `Assets/Resources/Discesa/simready_crate_trial.glb` (runtime copy via `DiscesaAssetWire.Wire`). Downloaded with `hf cp hf://datasets/codepawl/simready-glb/glb/crate_white_a14.glb`. No other dataset files used.
- **GLTFast package**: `com.unity.cloud.gltfast 6.17.0` (Unity glTFast, Apache-2.0), added in `Packages/manifest.json`, provides the `.glb` ScriptedImporter. Verified to resolve and compile under Unity 6000.6.0f1. Versions `6.21.0`/`6.22.0` were tried first and the registry returned "cannot be found" — `6.17.0` is the last resolvable version.

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
