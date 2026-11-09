# Viral AI-assisted game development — research note

Compiled 2026-11-08 for inspiration. Claims are attributed to the linked coverage; numbers
are the creators' own reports unless stated. This is a study note, not a benchmark.

## What went viral, and why

| Case | What it is | Reported outcome | Pattern worth stealing |
|---|---|---|---|
| **“Claude of Duty”** (Matt Shumer, browser FPS, Three.js) | One long orchestration prompt; Claude Code split the game into ~11 subsystems and ran builder + blind critic agents against real gameplay footage. | ~55,000 lines, no hand-made models/textures/audio, millions of views, 1,500+ stars / 250+ forks in three days. Critique score rose from 3.59/10 to ~5/10 — honest, not AAA. | **Gauntlet loop**: builder produces a checkable artifact; a fresh-context critic compares it to a reference; one biggest gap goes back; hard stop on time/tokens/rounds. |
| **Godogen** (htdt, Godot) | Claude Code skills that design, generate assets, write GDScript and test the game visually. | 337 points on Hacker News; demos are raw single runs, not cherry-picked. | **Visual QA agent**: screenshot-only critic (Gemini Flash) compares the running engine to a reference image and catches z-fighting, floating props, bad layouts that text review misses. |
| **“Granny vs Rent”** (non-programmer, Godot) | Spanish-language prompts; Claude wrote Blender Python for models, a bot test battery, a screenshot key and colour-measuring scripts. | Free playable demo on itch.io; built in ~22 rounds. | **Screenshot key + colour measurement**: automated checks that enemies stay visible and frames are not black. |
| **Iron Curtain Call / Whale Dev Studios** (Godot FPS) | One lead agent session plus feature sessions on branches; design docs as the only creative source; merge gate = headless bot playthrough + nav/geometry checks. | 3,929–4,558 commits in ~20 working days; free Steam demo 23–28 Oct 2026. | **Merge gate before anything lands**; agents do not invent story or layout, they flag gaps for the human. |
| **Spore self-checking game experiment** | Agent built a game, wrote five bot archetypes (idle, greedy, fixed, steward, seer), tuned from results, then a second turn searched 200 seeds. | Targets were met first turn; second turn exposed questions the targets missed. | **Bots as acceptance tests**: idle must lose, greedy rarely win, steward win in a range; report what the targets leave out. |
| **fly.pieter.com** (Pieter Levels, browser flight sim) | Cursor + Three.js; first playable in ~3 hours, built live on X. | $87K MRR / “$1M ARR in 17 days”, 320K players; later reported at $0/month. | **Build in public + instant play**: attention, not polish, drove the spike; revenue was ads/one-offs, not durable. |
| **Vibe Jam 2025/2026** | Jam requiring 80%+ AI-coded, browser-playable games; judges included Karpathy and mrdoob. | 1,000+ entries in 2025; $25K prize pool in 2026. | **Constraints create virality**: instant play, no login, visible AI provenance. |

Sources: `ournationonline.com/gauntlet-loop-claude-builder-critic-prompt-explained/`,
`news.ycombinator.com/item?id=47400868` (Godogen), `kblip.com` (Granny vs Rent),
`usecases.sh` (Iron Curtain Call), `dev.to/clbrge` (Spore experiment),
`levels.io/fly-pieter-com-vibecoded-flight-simulator`,
`onepromptman.ai` (Vibe Jam). Dates are as published; treat the numbers as creator claims.

Counterpoint worth keeping: the Korean coverage of the FPS notes that shooters are
heavily represented in training data, so “one-shot” results may be recombination rather
than novel reasoning. Treat viral demos as workflow evidence, not capability proof.

## What this means for Discesa

1. **We already have the hard half of the loop.** `PhotoDirector` gives 13 deterministic
   poses and 1280×720 captures. The missing half is a *blind visual critic*: run a vision
   model over `Screenshots/` with a fixed bar (HUD legible, reticle centred, turret visible,
   no black frame, no floating geometry) and send only failures back. This matches the
   Gauntlet and Godogen patterns without new engine work.
2. **Turn PlayMode tests into a bot playthrough gate.** Today 16 PlayMode tests cover
   movement, turret LOS, walker burst, pickups and poses. Add one scripted run that starts
   at spawn, flies the corridor, kills the turret, destroys the core and restarts; assert
   shields lost, time-to-kill and that no wall is escaped. That is the Iron Curtain Call
   merge gate at our scale.
3. **Make art generation reproducible.** Textures and HUD sprites are already generated
   from code in `ProceduralTextureBaker` / `HudArt`; keep that as the contract. AI-generated
   textures can be promoted copies with a manifest, never raw `GeneratedAssets/`.
4. **Screenshot stats are cheap insurance.** Port the Granny vs Rent trick: compute mean
   luminance and the presence of HUD accent colours per pose, fail the pose if the frame is
   too dark or the enemy silhouette is missing. Catches the exact failures we fixed by eye
   this session (invisible walker, blown ceiling light, pickup lost in its glow).
5. **Keep the human in the fun loop.** Every credible case says agents cannot judge fun:
   Iron Curtain Call needed four level versions and a human-added section; Shumer's critic
   never beat Call of Duty. Our `docs/PLAYTEST.md` remains the real gate.
6. **Devlog the evidence, not the promise.** The viral cases shipped a playable artifact and
   a visible build trail. Three reviewed captures per milestone plus a smoke log is a better
   story than a feature list — and it is already in `docs/evidence/`.

## Concrete next experiments (ordered)

- [ ] Add `tools/visual-qa.ps1`: per-pose luminance + HUD-colour presence, writes a pass/fail
      table next to `docs/evidence/`.
- [ ] Add `BotPlaythrough` PlayMode test with scripted input and win/restart assertions.
- [ ] Add a `photo_critic` prompt/template that receives one capture plus the pose name and
      returns pass/fail against a written bar; run it after every capture batch.
- [ ] Record a 60-second gameplay clip from the player build for the devlog (the one thing
      screenshots cannot show).
