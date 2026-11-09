# Discesa

An original, noncommercial six-degree-of-freedom arena prototype in Unity 6000.6.0f1 (URP),
informed by a behavioural study of the Descent 1 source release. No Descent code, art,
music, level data, names or branding is included; see `docs/REFERENCE.md` and
`docs/PROVENANCE.md`.

## What exists

A complete M0–M5 slice:

- cockpit overlay with console, portrait, reticle and a shield/energy/weapon/ammo HUD;
- 6DoF ship movement with fixed-substep physics, swept collision and arena containment;
- three rooms, two corridors and a service alcove, built from textured procedural geometry;
- a turret guard and a floor-walking burst enemy, both with line-of-sight checks;
- a destructible core objective, win/lose/restart loop, damage vignette and pickup messages;
- three original weapons (Pulse, Spread, Heavy) with pooled projectiles, trails and sparks;
- two pickups (energy cell, missiles) that respawn after 20 s;
- 13 deterministic screenshot poses for evidence capture.

## Run it

Open the folder in Unity Hub with 6000.6.0f1 and press Play in any scene — the game
bootstraps itself. A built player is at `Build/Discesa.exe`.

| Action | Keys |
|---|---|
| Pitch | `W`/`S` or `↑`/`↓` |
| Yaw | `A`/`D` |
| Roll | `Q`/`E` |
| Thrust | `I`/`K` |
| Strafe | `J`/`L` (hold `Shift` to make yaw strafe) |
| Vertical | `PageUp`/`PageDown` |
| Fire | `Space` or left mouse |
| Weapons | `1` Pulse, `2` Spread, `3` Heavy, `Tab` cycle |
| Camera | `V` toggles cockpit / beauty |
| Restart | `R` |

## Verify

With the Editor closed:

```powershell
unity test . --mode EditMode     # 21 tests
unity test . --mode PlayMode     # 16 tests
unity build . --target StandaloneWindows64 --output-path Build/Discesa.exe --allow-dirty-build
```

With the Editor open, use the Pipeline equivalents:

```powershell
unity command test run --mode EditMode --async-tests; unity command test status
unity command test run --mode PlayMode --async-tests; unity command test status
unity command build run --target StandaloneWindows64 --output-path Build/Discesa.exe --confirm
```

Latest verified result: 21/21 EditMode, 16/16 PlayMode, build succeeded with 0 errors.
The 15-second player smoke log is `Logs/player-smoke.log`.

## Evidence

Reviewed captures for every milestone are committed under `docs/evidence/M1..M5` (M1 cockpit
and damage, M2 rooms, M3 turret/projectile/pickup/walker, M5 win/lose/restart). Working
captures and logs stay gitignored.

## Documentation

- `docs/STATUS.md` — what works, current numbers, open items, resumable state.
- `docs/INSPIRATION.md` — viral AI-assisted game-development cases and what to adopt next.
- `docs/PLAYTEST.md` — the human playtest sheet (not yet filled in).
- `docs/PROVENANCE.md` — reference licensing, asset record, AI-credit ledger.
- `docs/REFERENCE.md` — what was studied and where the original implementations live.
