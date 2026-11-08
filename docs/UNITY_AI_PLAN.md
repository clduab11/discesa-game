# Unity AI credits: plan for 1,000

Source: https://docs.unity.com/en-us/ai/credits/credits-about (read 2026-11-08, page updated 3 days prior).
Every number below is Unity's own "representative example", not a fixed rate. The page states
consumption varies with project size, context, prompt complexity, conversation length, mode and
model, and that Unity may change rates without notice. **The Unity Dashboard is authoritative.**

## Mechanics that change how you spend

- **Reservation, then charge.** A job reserves the maximum it could cost. Upscaling "consumes about 26
  credits [but] reserves 183". The balance must cover the reservation or the job will not start.
- **Failures and content-filter blocks are free.**
- **No rollover.** Unused credits vanish at the billing cycle start; at zero, AI pauses.
- **Multiple outputs multiply the cost.** Four images ≈ four jobs.
- **Assistant cost scales with conversation length and mode**, not just the request.

## Verified cost table (representative)

| Action | Credits |
|---|---|
| Assistant message, any model | 4 to 1500+ |
| **Unity Lite** — short question / API lookup | ~4 / ~5 |
| Lite — simple script, explanation, feature plan | ~8 / ~13 / ~13 |
| Lite — architecture brainstorm, Editor tool | ~25 / ~25 |
| Lite — debug and fix across the project | ~35 |
| Lite — detailed implementation plan | ~48 |
| Lite — full gameplay mechanic | ~58 |
| Lite — refactor a complex system | 100+ |
| **Unity Default** — simple scene / controller / advanced city | ~50 / ~190 / ~520 |
| **Unity Ultra** — simple / moderate / involved / advanced | ~150 / ~450 / ~700 / ~1500 |
| Generate image | 1 to ~47 (style models ~8; FLUX.2 Pro 6–7; GPT Image ~18–25; Gemini 11–45) |
| Generate texture (PBR maps ~7, realistic or hand-painted ~9, Gemini ~12, Scenario ~25) | 7 to 25 |
| Generate 3D model | 38 to 194 (Tripo 3.1 ~38, Tripo P1 ~84, Tripo P2 185–194) |
| Rig a character | 42 to 59 |
| Retopologise / retexture | 13 to 95 |
| Sound effect | ~26 flat, any length |
| Music | 6 to 13 |
| Speech | ~29 per 1,000 characters, up to ~290 |
| Animation from text | 49 to 122 (~12/second) |
| Skybox | ~12 |
| Upscale a skybox | ~26 |
| Video, 5 s | 95 to 480 |

## Allocation: planned 653 of 800 soft cap, 347 held back

Authorised 1,000; project soft cap 800. Plan spends **653**, which leaves 147 under the cap and 347
of the authorised total untouched as a buffer for rate drift or re-runs.

| Bucket | Unit cost | Quantity | Credits | Why |
|---|---|---|---|---|
| Assistant on Lite: plans + debug | 48 / 35 / 25 / 13 | 3 plans, 4 debug, 2 tools, 3 explanations | **350** | The only place AI beats deterministic tooling: architecture, tricky Unity API behaviour, Editor tooling |
| Sound effects | ~26 | 6 | **156** | We have zero audio; 6 one-shots covers fire, hit, explosion, thrust, alarm, UI |
| Music | 6–13 | 2 | **26** | Menu + combat loop, cheap at this price |
| Skybox / arena ambience | ~12 | 2 | **24** | Gives the graybox depth without hand-making HDRIs |
| UI icons / style images | ~14 | 4 | **56** | HUD icons if procedural art stops being enough |
| Textures | ~9 | 4 | **36** | Only if a specific surface is needed; procedural textures are free |
| 3D model generation | 38–194 | **0** | **0** | Deliberately zero — see below |
| **Total** | | | **653** | |

## Rules

1. **Never generate a 3D model with credits.** `tools/blender-lpm` builds game-ready meshes from
   primitives for free, deterministically, with PASS/FAIL gates and a triangle budget. A 38–194
   credit Tripo result would be un-gated, un-versioned and stylistically inconsistent with the
   existing ship. This is the single biggest saving in the plan.
2. **Never use Unity Ultra** (150–1500) unless a specific task has already failed twice on Lite and
   Default. Planning on Lite is ~13–48; on Ultra it is ~150+.
3. **Stay on Lite for almost everything.** Default is reserved for one architectural review (≈50).
4. **Short conversations.** Cost scales with conversation length. One topic per session, paste only
   the files the task needs, start fresh rather than dragging a long thread.
5. **Spend early in the billing cycle.** Credits do not roll over.
6. **Respect reservations when sequencing.** A 194-credit job reserves more than it charges; do not
   queue expensive asset jobs with a nearly-empty balance.
7. **Failures are free, but do not farm them.** Retry a blocked job once, then change the prompt.

## Operating model (honest constraint)

Unity AI (the Assistant panel in the screenshot) is an in-Editor GUI. I cannot click it, and I will
not claim a run I did not perform. The split is:

- **I write** the brief, the exact scope, the files to attach, and the acceptance test for the output.
- **Chris runs** the prompt in the Assistant and reports the credit count from the dashboard.
- **I verify** the result in the repo: does it compile, do the tests pass, does the smoke run still
  start, does the diff do only what was asked.

Every spend gets a row in `docs/CREDIT_LEDGER.md`: date, task, model, credits reserved, credits
charged, outcome (accepted / reverted), and what it changed.

## First three spends, when approved

1. **Architecture review on Default (~50)** — hand it `ShipController.cs`, `ShipPhysics.cs` and
   `docs/STATUS.md`, ask only for failure modes in the swept-move and no-tunnel logic. This is the
   highest-risk code in the project and a second reader is worth 50 credits.
2. **Skybox (~12)** — one arena skybox matching the palette, since the interior currently has none.
3. **Six SFX (~156)** — after the playtest, because the playtest decides which sounds matter.

Total for those three: **~218**, leaving 435 under the soft cap.
