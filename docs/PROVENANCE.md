# Provenance

## Reference code (study only, nothing copied into this repo)
- Descent 1 source release: https://github.com/videogamepreservation/descent , pinned commit `f6fb1c30a06725b403dfa7db4b251ecea1c3efc1`.
- The pinned study copy is **not vendored and not committed**. Optional local checkout: `pwsh -File tools/fetch-reference.ps1` writes to gitignored `reference/descent/`. See `docs/REFERENCE.md` for the full file-by-file study record.
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

## Procedural art added for the M0–M5 slice (2026-11-08)

- **Textures** — `Assets/Art/GeneratedFallback/Textures/` (`wall_panel`, `floor_grate`,
  `ceiling_panel`, `hazard_stripe`, `metal_trim`, `core_trim`, `prop_metal`). Generated
  deterministically by `Assets/Scripts/Discesa/Editor/ProceduralTextureBaker.cs` from code
  only; no photographs, no third-party textures, no AI generation. Original to this repo.
- **Materials** — `Assets/Resources/Discesa/*.mat`, baked by
  `Assets/Scripts/Discesa/Editor/MaterialBaker.cs`. Kept under `Resources` so the built
  player does not depend on `Shader.Find` for stripped URP shaders. Original palette.
- **HUD sprites** — generated at runtime by `Assets/Scripts/Discesa/HudArt.cs` (panels,
  brackets, weapon icons, portrait). Original.
- **Sound** — none in this slice.

## AI-generation ledger

- Unity AI credits authorised: 1,000 (soft cap 800). **Spent: 0.** No texture, mesh, audio
  or code was generated through Unity AI for this slice.
- `GeneratedAssets/` stays gitignored; any future AI output is promoted by hand into
  `Assets/Art/Generated/` with a row in this file. Nothing has been promoted yet.
- The reference study remains behavioural only. `descent-master/` (a local unpacked copy
  of the pinned source) and `reference/descent/` (the opt-in script checkout) are both
  gitignored and are not part of the build or the repository history.
- All runtime movement, combat, HUD, arena and capture code under `Assets/Scripts/Discesa/`
  is original C# written for this project. No Parallax code, names, level data, art,
  music or branding is present.

## Vendored third-party code (2026-11-08)

- **`tools/blender-lpm/`** — Blender Low Poly Modelling Skill.
  - Source: https://github.com/ozanzeng/blender-LPM-skill
  - Pinned commit: `687d0a46f1280bc7d11dbf83539840100ec909dc`
  - Licence: MIT, © 2026 Ozan Zengin (full text kept at `tools/blender-lpm/LICENSE`).
  - Vendored subset: `SKILL.md`, `references/` (budgets, Unity export contract, QA checklist,
    recipes), and the deterministic local scripts (`lpm.py`, `lpm_kit.py`, `bl.py`,
    `render_views.py`, `inspect_scene.py`, `gate_report.py`, `beauty_render.py`,
    `compare_silhouette.py`, `asset_card.py`) plus two recipes as patterns.
  - Deliberately **not** vendored: every script that calls fal.ai, Hunyuan weights, or needs
    reference turnarounds (`make_turnaround.py`, `multiview_to_mesh.py`, `hy3d_local_mv.py`,
    `bake_registered.py`, `project_views_bake.py`, `atlas_from_views.py`, `register_views.py`,
    `shape_from_views.py`, `score_views.py`, `find_view.py`, `calibrate_elevation.py`,
    `gen_to_lowpoly.py`, `hull_*.py`, `render_n_views.py`, `render_ref_views.py`,
    `render_unlit_views.py`, `pbr_from_basecolor.py`, `wire_pbr_export.py`, `turntable.py`).
    Those either cost money or need assets we have not made.
  - Modifications: none to the scripts. Our recipes live outside the vendored tree.
  - Why: it produces Unity-axis FBX with a mask map, enforces a triangle budget, and emits a
    PASS/FAIL gate report — the same "prove it" discipline the rest of this repo uses.

## Reference images (Venice.ai)

- Venice.ai is used **only** to generate concept art and orthographic turnarounds that feed a
  Blender recipe. Reference images are inputs, never shipped assets.
- API docs and model prices read 2026-11-08: `docs/REFERENCE_IMAGES.md` records the model IDs,
  seed and payload for each brief.
- Any generated reference must get a row here: brief, model ID, seed, cost, and the asset it fed.
  **None generated yet.**
- API keys stay in Chris's environment; never in the repo, a prompt or a log.

## Unity AI

- Credits: 1,000 authorised, 800 soft cap. **Spent: 0.** Plan and per-spend ledger live in
  `docs/UNITY_AI_PLAN.md` and `docs/CREDIT_LEDGER.md`.
- Policy: no 3D model generation credits (Blender LPM is free, gated and deterministic); no
  Unity Ultra model without a documented double failure on Lite and Default.
