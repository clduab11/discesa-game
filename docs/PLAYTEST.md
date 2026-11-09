# Playtest sheet

Human gate. Nothing here counts as done until a person has actually played the build and
filled this in. The agent-written evidence is screenshots, tests and a smoke log only.

## Build under test

- Player: `Build/Discesa.exe` (StandaloneWindows64, Unity 6000.6.0f1)
- Run: `Build\Discesa.exe -screen-width 1280 -screen-height 720 -screen-fullscreen 0 -logFile Logs\playtest.log`
- Session length: 10–15 minutes, fresh start, then at least one loss and one win.

## Session log

| Date | Build | Setup | Result | Notes |
|---|---|---|---|---|
| | | | | |

## Script

1. **Cockpit check (1 min).** Do not move. Confirm the console, portrait, reticle and all
   four gauges are readable, and that no ship hull or canopy edge blocks the view.
2. **Comfort (3 min).** Orbit, strafe and make three 180° turns. Use `V` once to see the
   external camera, then return to cockpit.
3. **Traversal (2 min).** Fly from Entry Hall through Corridor A, Relay Gallery, Corridor B
   into the Core Chamber. Clip every doorway at speed; no stop-dead, no escape.
4. **Combat (3 min).** Kill the turret with Pulse, then switch to Spread and Heavy. Confirm
   energy drain, missile count, cooldowns and hit feedback read correctly.
5. **Enemies (2 min).** Let the walker burst hit you once. Confirm the damage vignette is
   visible but does not hide the HUD.
6. **Pickups (1 min).** Collect the cell with energy below max and the missile with fewer
   than six rounds. Confirm the message and the 20 s respawn.
7. **Objective (2 min).** Destroy the core. Confirm `CORE DESTROYED` and `R` restarts clean.
8. **Loss (1 min).** Let the turret/walker kill you. Confirm `SHIP DESTROYED` and `R` restarts.

## Checkpoint criteria

Rate 1–5 unless marked pass/fail.

1. **Comfort** — five minutes of continuous 6DoF motion without motion sickness or losing
   orientation. Score:
2. **Cockpit readability** — gauges legible at 1280x720; reticle does not obscure targets.
   Score:
3. **Weapon feel** — each weapon is distinguishable by rate, spread and cost; hits register.
   Score:
4. **Enemy readability** — turret identifiable from 20 m; walker identifiable at 8 m; incoming
   fire visible before it lands. Score:
5. **Pickup clarity** — both pickups are identifiable from 5 m and the message is readable.
   Score:
6. **Collision** — every wall and prop at full speed slides, never stops dead or escapes.
   Pass / fail:
7. **Loop** — win, lose and restart all work in one session without a scene reload.
   Pass / fail:

## Findings to hand back

- Bugs (what happened, where, reproducible steps):
- Balance notes (time-to-kill, damage taken, energy starvation):
- Anything that made you stop having fun:
