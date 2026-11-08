# Discesa

An original, noncommercial six-degree-of-freedom space-arena prototype in Unity, informed by
a study of the Descent 1 source release. No Descent code, art, music, levels, or branding is
included.

## What exists right now

A playable graybox slice: one enclosed arena, 6DoF ship movement with swept collision, one
turret enemy, one weapon, one objective, and a win / lose / restart loop. Everything is built
from Unity primitives at runtime.

## Run it

Open the folder in Unity 6000.6.0f1 and press Play in any scene. The game bootstraps itself.

Controls: `I`/`K` thrust, `J`/`L` strafe (or `Shift`+`A`/`D`), `PageUp`/`PageDown` lift,
`W`/`S` pitch, `A`/`D` yaw, `Q`/`E` roll, `Space` fire, `R` restart.

A built player is at `Build/Discesa.exe`.

## Verify

    unity test . --mode EditMode     # 5 tests
    unity test . --mode PlayMode     # 4 tests

## Documentation

- `docs/STATUS.md` — what works, current numbers, open items, resumable state.
- `docs/PROVENANCE.md` — reference licensing and asset record.
- `docs/PLAYTEST.md` — the human playtest sheet (still empty).
- `docs/PLAN.md` — scope, engine decision, dataset annex.
