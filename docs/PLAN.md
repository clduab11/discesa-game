# Discesa — First Playable Plan (rev 2)

Noncommercial, non-monetized, Desktop-first 6DoF prototype. Unity + Blender. Goal: playable graybox, not a design doc.
Rev 2 adds: HF game-asset dataset research annex (verified 2026-11-07/08) and a C#/Godot language+engine bake-off.

## 0. Inventory (verified 2026-11-07)

- Repo `C:\Users\cld-main\Desktop\github-projects\discesa-game` — empty, no `.git`, no Unity project. Target remote `clduab11/discesa-game` is clean (not cloned locally).
- Unity: **6000.6.0f1** (`C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe`), CLI 1.0.0-beta.17. Also 6000.3.22f1 installed. No Editor open. `unity mcp configure --list` has no Kilo entry — Unity MCP deferred per user decision 2026-11-07.
- Blender: **5.2.1 LTS** (`C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`). No Blender MCP configured — deferred.
- Node v26.7.0, Python 3.14.3, Git, winget. npm globals: claude-code, kilocode/cli, hostinger-api-mcp, vercel. **No** `rea-agents`, `bla-agents`, Ghidra/Hopper/IDA/Kuna, or Descent executable found. **No Godot installed.**
- MCP servers available in Kilo now: github, huggingface, deepwiki, context7, firecrawl, etc. No Unity/Blender MCP connected.
- Hardware: RTX 4060 Ti 8 GB VRAM, 48 GB RAM.

No installs, accounts, pushes, publishes, or Linear changes without approval.

## 1. Game vision — minimal playable

Single enclosed arena, 6DoF ship (translate + rotate on all axes), wall collision, one enemy, one weapon, one objective (destroy enemy core), win/lose/restart loop. Desktop input now; input abstraction (`IShipInput`) so mobile touch/gyro can be swapped later. Primitives only for slice 1. No campaign, multiplayer, procedural world, or bulk assets.

Not a Descent clone — original ship, arena, art, and code informed by physics structure only. No Parallax/Interplay names, art, music, levels, or branding.

## 2. Source / REA investigation + rights

### Descent 1 reference
- Repo: https://github.com/videogamepreservation/descent — pinned HEAD `f6fb1c30a06725b403dfa7db4b251ecea1c3efc1` (seen in API URLs 2026-11-07).
- License from `README.TXT`: Parallax Software 1993–1998, royalty-free perpetual license **for noncommercial, royalty/revenue-free use only**. "IN NO EVENT SHALL THE END-USER USE THE COMPUTER CODE CONTAINED HEREIN FOR REVENUE-BEARING PURPOSES."
- Source is Descent 1 v1.5 (minus sound/modem). **No** grant for game art/music/levels/data/branding. Watcom C, MASM, DOS.
- Rule: any code copied/adapted must keep the Parallax footer and be recorded in `docs/PROVENANCE.md` with file, commit, and change summary. Commercial change → re-review. Do not import game assets.

### What the source demonstrates (study order)
1. `MAIN/PHYSICS.C` `do_physics_sim` / `do_physics_sim_rot` — thrust/drag split into 1/64s fixed steps, FVI segment sweep, retry loop, wall slide vs stick vs bounce, object-object collide, re-derived end-of-frame velocity. Rotational: `rotvel`/`rotthrust` + angular drag + `turnroll` banking (`PF_TURNROLL`, `set_object_turnroll`, `TURNROLL_SCALE`).
2. `MAIN/CONTROLS.C` `read_flying_controls` — thrust = `fvec*forward + rvec*sideways + uvec*vertical`, scaled by `max_thrust / FrameTime`; rotthrust from pitch/heading/bank times.
3. `MAIN/OBJECT.H` `physics_info` — the data contract to modernize: velocity, thrust, mass, drag, rotvel, rotthrust, turnroll, flags (TURNROLL/LEVELLING/BOUNCE/STICK/PERSISTENT/USES_THRUST).
4. `VECMAT/` — instructive fixed-point math; do not port wholesale.
5. Collisions: `FVI.C` (find_vector_intersection) + `COLLIDE.C` — swept segment-based; Unity equivalent is our own substep + cast loop.

### Modernize, don't clone
- Custom kinematic stepper mirroring Descent's drag-then-move-then-slide loop (no PhysX dependence, no tunneling, tunable slide friction; no bounce in slice 1).
- Banking subtle and optional; no auto-levelling in slice 1.
- Motion math lives in plain, engine-agnostic C# classes (see bake-off) so the controller is portable.

### REA / BLA
- REA (`morluto/rea`, MIT): MCP/CLI binary analysis (Node 22/24/26 + Hopper/Ghidra/IDA/Kuna for native). **Not installed; not needed** — source answers flight questions. Use later only for a specific unresolved question on a lawfully obtained executable (DOS MZ/LE format), after approval. Record REA evidence separately from source citations. Never claim binary analysis that didn't happen.
- BLA (`qybaihe/bla`, MIT, `bla-agents`): defensive anti-REA obfuscation of release artifacts. No gameplay value. Defer until prototype + public-release threat + isolated test on our own artifacts. Never in gameplay code.
- Rust verdict: research-only (REA/BLA tooling). Not the game runtime — Bevy would add a third toolchain with no editor integration and no MCP on this box, against the smallest-playable goal.

## 3. Engine & language bake-off (C# / Godot / GDScript / Rust)

**Primary: Unity 6000.6.0f1 + C#.** Rationale: verified installed, `unity test` CLI automation, Input System package for the abstraction, URP for 6DoF readability aids, an official `unity mcp` bridge for later, and C# portability of engine-agnostic motion math.

**Bake-off (step 0.5, ≤1 session, no default installs):** a scored comparison on:
1. 6DoF controller ergonomics (identical — our kinematic math is plain classes in C#; GDScript only if Godot chosen).
2. Blender round-trip: Blender exports GLB natively; Unity needs `com.unity.formats.gltf-tools` (GLTFast) — a proven, free import path for the HF datasets below; Godot imports glTF first-class (and `codepawl/simready-glb` ships a Godot importer folder).
3. HF dataset ingestion (all candidates ship GLB — see annex): test one GLB from `codepawl/simready-glb` into Unity via GLTFast; if Godot is approved for install, same file via its importer. Compare materials, colliders, scale.
4. Test automation + CI: Unity PlayMode tests + `unity test` (installed) vs Godot headless (GUT/gdUnit4, needs install).
5. MCP support today: `unity mcp` exists (needs Editor open); Blender MCP unconfigured on both paths.
Decision checkpoint: keep Unity+C# unless the GLB import trial or 6DoF comfort work exposes a blocking Unity cost; Godot+C# remains a documented fallback (Godot 4 supports C#/.NET), GDScript only on a full engine switch. **No Godot download without approval.**

**Blender stays the authoring tool** for ship/enemy graybox meshes regardless of engine: GLB out (scale m, transforms applied, smooth normals), which is also the interchange format of every dataset in the annex.

## 4. Sequenced implementation plan

Each step: acceptance criteria → implement → run `unity test` + manual play → report evidence. Human review checkpoint before destructive editor actions.

0. **Repo + provenance skeleton** — git init, `.gitignore` (Unity), `docs/PROVENANCE.md`, `docs/STATUS.md`, pin Descent commit. 
0.5. **Engine bake-off trial** (see §3) — import one SimReady GLB via GLTFast as evidence; record in STATUS.md; confirm/switch engine.
1. **Input abstraction** — `IShipInput` (Pitch/Yaw/Roll/Thrust/Strafe/VertTranslate/Fire), `KeyboardMouseShipInput`, `DebugScriptedInput` for tests. Criteria: same inputs drive Play and PlayMode tests; no `Input.*` in ship code.
2. **Ship controller** — `ShipPhysics` ScriptableObject-tunable (mass/drag/max thrust/max rotthrust/rot drag), fixed 60 Hz substeps, sweeps + slide. Tests: terminal velocity under constant thrust, exponential drag decay, rotation rates match spec, no tunneling at max speed on thin walls.
3. **Arena** — enclosed ~40×20×40 m box + pillars, BoxColliders, primitive materials. Criteria: slide on all 6 planes, no escape, <1 mm rest penetration.
4. **Enemy + weapon + objective** — one turret/drone, fast projectile or hitscan, 100 HP ship / 50 HP enemy, win/lose/restart (R). HUD: HP, speed, objective, crosshair.
5. **Playtest checkpoint** — orbit, strafe, 180° turns without nausea; hit registration readable at 20 m; collision feedback clear. Record in `docs/PLAYTEST.md`.
6. **Asset experiments unlocked** — only after checkpoint 5: HF-dataset prop trials (annex), Blender GLB mini-loop (brief → model → inspect → export → GLTFast import → prefab → visual inspect), TripoSR single-image test. No mobile work until the loop is fun.

## 5. HF game-asset dataset research annex (licenses verified via HF API 2026-11-07/08)

Research method: HF Hub search (tags `sprites`, `3d`, `pixel-art`, `game-assets`) + user-supplied shortlist; every card below pulled live and license read from metadata, not from memory or summaries.

### 3D models — relevant to Blender/Unity asset pipeline
| Dataset | License (verified) | Contents | Discesa verdict |
|---|---|---|---|
| `codepawl/simready-glb` | CC-BY-4.0 (credit NVIDIA) | 2,595 web-ready GLB of NVIDIA SimReady props; LODs, phone-friendly + web-compressed variants; joints on 929 models; Unreal/Godot importer folders; ~3.2 GB | **Primary 3D trial** for step 0.5/6. Realistic industrial props fit a mine-station arena; import 1–5 files only |
| `nvidia/simready-assets` | CC-BY-4.0 | 2,677 OpenUSD props, 490 GB, physics data | Skip bulk download; source of provenance for above |
| `nvidia/USDCraft-10K` | Apache-2.0 | Articulated USD objects (hinges, doors, joints) | Reference for interactive props later; USD→Unity path unproven |
| `scenario-labs/3d-collection` | CC-BY-4.0 (credit "Scenario") | 1,342 AI-generated game-style assets, GLB/FBX/Blend/VOX, ~39 GB | Good stylized-look reference; try 1–2 GLB after checkpoint 5 |
| `ccore/ThreeJS-3D-GLB-Assets` | MIT | 3,918 validated GLB + three.js scenes | Browser-targeted, but MIT GLBs individually usable |
| `allenai/objaverse` / `-xl` | ODC-By dataset-level; per-object varies (Objaverse 1.0: 721K CC-BY, 25K CC-BY-NC, etc.) | 800K / 10M+ objects | Inspection only; per-object license filter mandatory before use |
| Poly Haven | CC0 | HDRIs, models, textures | Safe for arena lighting/materials later |
| NVIDIA SAGE-10k | Apache-2.0 | 10K scenes, 565K objects | Scene-layout research only; no bulk download |
| TripoSR (model, MIT) | MIT | image→3D, ~6 GB VRAM | Optional single-image→mesh experiment on this 8 GB GPU; not production |

### 2D sprites / pixel art — HUD/icon research only (Discesa is a 3D game)
| Dataset | License (verified) | Notes |
|---|---|---|
| `Angais/pixel-art-atlas-1.4m` | CC0 (metadata), **but generated with Krea 2 Turbo — upstream generator terms caveat noted on card** | 1.4M 16×16 sprites; captions unreviewed; single-file pulls only |
| `unstonio/pixelgpt-24x24-20k` | CC-BY-4.0 | 20k rights-conscious 24×24 subset; verified 9.8K downloads (user note said ~565 — counts grew) |
| `carlosuperb/lpc-4view` + `lpc-action` | CC-BY-SA 3.0 | LPC-style character sheets; undergrad research project; share-alike makes derivatives virally licensed — use for reference, not shipped art |
| `azyg0s/pixelart-172k` + `pixelart-50m` model | MIT (both) | 32×32 icons; 49.8M-param model runs on CPU — cheapest concrete icon-generation experiment |
| `sprited/sprite-dx-data` | **No license in dataset metadata (verified absent)** | Treat as unlicensed — do not use without contacting author; useful only as matting/loop-annotation research reference |
| `Fraser/pico-8-games` | CC-BY-NC-SA 4.0 (scraped from Lexaloffle BBS) | Noncommercial-compatible with project, but third-party game content — code/art study only, never shipped |

### Corrections vs. the user-supplied shortlist (cross-checked live)
- `unstonio/pixelgpt-24x24-20k` is CC-BY-4.0 (not unspecified); downloads now 9.8K.
- `azyg0s/pixelart-172k` is MIT (not unspecified) — the friendliest sprite set.
- `sprited/sprite-dx-data` carries no license tag at all — flagged, excluded from use.
- `Fraser/pico-8-games` is CC-BY-NC-SA 4.0 — NC is compatible here, but scraped community games carry takedown-style rights risk; study only.
- `nvidia/USDCraft-10K` is Apache-2.0 (shortlist implied CC-BY family).

### Usage rules (all datasets)
1. Nothing bulk-downloaded; `hf download` of individual files only, after checkpoint 5 (or the single bake-off GLB for step 0.5).
2. Every used asset gets a `PROVENANCE.md` row: source repo, license, attribution text (NVIDIA / Scenario / artist), modifications, and for AI-generated content the generator + prompt lineage where the card provides it.
3. Import gate per asset: scale, topology, normals, UVs, material conversion (USD/Blender→GLB→Unity), texture size, colliders, draw-call cost. Fail → back to primitives.
4. CC-BY-SA (LPC) and NC items are reference-only. No scraping/training on sprite datasets; the only ML-generation experiments permitted are `pixelart-50m` (MIT, CPU icons) and TripoSR (MIT, one mesh), both offline/local, no paid services, no uploads.
5. AI-generated CC0 with unreviewed captions (Pixel Art Atlas) — treat captions as unverified; any shipped icon must pass human review.

## 6. MCP / tooling plan

- Kilo MCPs in use now: github, huggingface, deepwiki, context7, firecrawl (read-only research).
- Deferred, each needing explicit approval: `unity mcp configure` (needs Editor open; writes client config), Blender MCP addon, Godot install (bake-off), TripoSR/pixelart-50m Python env.
- No paid generation services; no uploading source, screenshots, or assets to external services.

## 7. Risks

1. 6DoF comfort/readability without banking aids — mitigate with playtest checkpoint and tunables.
2. Wall-slide tunneling — substeps + casts; tested at max speed.
3. GLB→Unity material fidelity (KTX/clearcoat from SimReady conversions) — single-file bake-off trial before any dependency on the path.
4. Dataset license drift — re-verify each card at point of use; provenance row per asset.
5. Engine-switch temptation mid-build — bake-off is step 0.5 and timeboxed; afterwards Unity+C# unless a blocker is documented.

## 8. Smallest first implementation step

Init repo + URP 3D project in `discesa-game/` + first commit (`README.md`, `PROVENANCE.md`, `STATUS.md`). Then step 0.5 bake-off trial (one SimReady GLB via GLTFast).

Needs approval: git init + `unity projects create` (URP), push to `clduab11/discesa-game`, adding `com.unity.formats.gltf-tools` to the project, and the one-file `hf download codepawl/simready-glb <file>` for the import trial. Next milestone after approval: steps 1–3, then pause for playtest review before step 4 polish and step 6 asset experiments.
