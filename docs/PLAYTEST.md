# Playtest sheet

Fill this in by actually playing. Nothing here is done until a human has run it.

## Session log

| Date | Build | Setup | Result | Notes |
|---|---|---|---|---|
| | | | | |

## Checkpoint criteria (must pass before asset work starts)

1. **Comfort** — five minutes of continuous orbiting, strafing and 180 degree turns without
   motion sickness or disorientation. Rate 1-5:
2. **Readability** — the red core is identifiable from 20 m, and an incoming shot is visible
   before it lands. Rate 1-5:
3. **Responsiveness** — the ship begins to turn on the same frame as the key press, and stops
   predictably when released. Rate 1-5:
4. **Collision** — hitting every wall and both pillars at full speed produces a slide, never a
   stop dead, and never an escape. Pass / fail:
5. **Loop** — win and lose banners are readable, and `R` restarts in under one second.
   Pass / fail:

## Specific questions to answer while playing

- Does the ship feel like it has mass, or is it a camera on a leash?
- Is 20 m/s terminal forward speed too slow, too fast, or right?
- Does releasing rotation coast too long? (rotDrag is 8 /s)
- Is the chase camera cutting into walls or pillars?
- Does the crosshair sit where the shots actually go?
- Can you tell when you have been hit, without reading the HUD?

## Changes implied by each answer

Record the tuning value to change, not just the feeling. For example: "turns feel floaty
after release" implies raising `rotDrag` from 8 to 12 in `ShipPhysics`.
