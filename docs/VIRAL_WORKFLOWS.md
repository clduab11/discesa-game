# AI-assisted game development: what actually ships

Research note, 2026-11-08. Sources read directly, not summarised from memory.

## Method and its limits

Primary sources: `lappemic/awesome-ai-built-games` (curated list of playable AI-built games),
`htdt/godogen` (an agentic game generator), Unity's AI credits and third-party-agent
documentation, and the Vibe Jam / AI Browser Game Jam directories.

Caveat stated by the list itself: entries are self-reported, "AI wrote most of each game's code"
is a claim by the submitter, and a curated list is not a sample. Virality is not quality, and a
jam winner is not evidence the approach scales to a six-month project.

## What the shipped work looks like

Of roughly 130 listed playable games, the overwhelming majority are **browser builds** on
Three.js, HTML5 Canvas, Phaser or PixiJS. Unity and Godot appear but are a small minority.
Vibe Jam 2026 alone reports over 2,000 entries across 2025–2026.

The nearest analogues to Discesa, all playable in a browser tab:

| Game | What it is | Stack |
|---|---|---|
| Null Range | Multiplayer first-person **space dogfights** with sonar, boost, lasers | Cursor · Three.js |
| Dyson Defender | Fly around a Dyson sphere, shoot waves of alien ships before they reach it | Cursor/Claude/Grok · Three.js |
| Stellar Drift | Mine asteroids, upgrade your ship, travel through wormholes | Three.js |
| FragByte | Arena shooter with katana parry and grappling hook | Three.js |
| Flight Simulator 2025 (`fly.pieter.com`) | Multiplayer flight sim, shared world, dogfights | Cursor + Grok 3 · Three.js |

Pattern: the successful ones are **one verb, one arena, instant load**. None of them ship a
campaign, and the space ones are all 3D but not 6DoF-with-roll.

## Workflow patterns worth stealing

1. **Proof over claims.** Godogen's stated design: the agent "judges results from the running game
   (a live URL or a recorded clip), not from a clean compile, so visible defects drive the next
   iteration." A green build is not evidence.
2. **A steerable artifact.** The same project offers two modes: watch the live game and steer at
   decision points, or leave it unattended and get a 15–20 s proof recording. The artifact is the
   review surface, not the chat transcript.
3. **Tiered asset generation.** Local and free by default; paid APIs only fill what the local tools
   cannot make. Assets are generated as part of the loop, not as a separate phase.
4. **Agent guardrails beat agent enthusiasm.** Godogen notes C# compiles type-check "the whole
   project before anything runs, so mistakes surface early" versus GDScript failing at runtime.
   `typeonce-dev/ritmo` is built on the same idea: "wrong code doesn't compile and the error names
   the fix."
5. **Deadline-shaped scope.** Jams (Vibe Jam, AI Browser Game Jam 5) force loop closure. The
   shipped games are small because the format demands it.
6. **Engine choice follows the viewing surface.** Browser wins when you want to watch and steer
   from anywhere; desktop engines win when the game must be a native build.

## What we adopt in Discesa

| Pattern | Concrete change | State |
|---|---|---|
| Proof over claims | Keep the headless player smoke run; add a screenshot capture from the built player as the visual proof artifact | smoke run done, capture not built |
| Gates before opinions | Unity tests + LPM gate report + silhouette IoU threshold | tests done, LPM gates vendored |
| Tiered assets | Primitives and LPM recipes first; Venice only for reference images; no paid 3D generation | in place |
| Type-checked guardrails | C# compiles, PlayMode/EditMode tests must pass before a build counts | in place |
| One verb | Thrust, shoot, destroy the core. No new mechanic until the playtest sheet is filled | enforced by scope |
| Engine follows viewing surface | Stay Unity desktop for 6DoF feel and input latency; revisit a web build only as a shareability experiment | unchanged |

## What we deliberately do not adopt

- **Browser-first rewrite.** Our differentiator is 6DoF flight feel with roll, which wants a native
  frame budget and a real input stack. Three.js space games in the list are not 6DoF.
- **Multi-agent studio swarms** (`Claude-Code-Game-Studios` and similar). Cost and coordination
  overhead exceed the value on a one-mechanic slice.
- **Backends, leaderboards, crypto, multiplayer.** Out of scope and against the project's
  noncommercial constraint.
- **Generated music and voice lines** before the flight loop is judged fun. Cheap to add later.
- **A proof video as a substitute for a human playtest.** A recording shows rendering; it does not
  show nausea, readability or responsiveness.
