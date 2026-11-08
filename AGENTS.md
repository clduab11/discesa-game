# Discesa — agent notes

Original, noncommercial 6DoF space-arena prototype. Unity 6000.6.0f1 (URP) + Blender 5.2.1.
No Descent code, art, music, levels or branding is in this repo. Do not add monetisation,
paid services, publication steps or accounts without asking Chris first.

## Read these before changing anything

- `docs/STATUS.md` — what works, current tuning, open items. Update it at the end of a session.
- `docs/PROVENANCE.md` — reference licensing and every asset's origin. Add a row for any new asset.
- `docs/PLAYTEST.md` — the human checkpoint. Never mark it done on someone's behalf.
- `docs/UNITY_AI_PLAN.md` + `docs/CREDIT_LEDGER.md` — before spending Unity AI credits.
- `docs/REFERENCE_IMAGES.md` — Venice.ai briefs, model IDs and prices.

## Commands

```
unity test . --mode EditMode
unity test . --mode PlayMode
unity build . --target StandaloneWindows64 --output-path Build/Discesa.exe
unity run . -- -executeMethod MaterialBaker.Bake
unity status
```

Blender LPM toolkit (headless, no API calls):

```
$env:BLENDER_EXE='C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
python tools/blender-lpm/scripts/bl.py --script <recipe>.py -- --out _work/<asset>/<Name>
python tools/blender-lpm/scripts/render_views.py -- --input _work/<asset>/<Name>.blend --out _work/<asset>/views --ortho
python tools/blender-lpm/scripts/inspect_scene.py -- --input <Name>.blend --out <Name>.inspect.json
python tools/blender-lpm/scripts/gate_report.py <Name>.inspect.json --class <class> --budget <n>
```

## Rules that have already cost us a session

- The project is **Input System only** (`activeInputHandler: 1`). Legacy `Input.GetKey` throws and
  is easy to swallow in a `try/catch`, which silently disables controls.
- Unity 6's `BoxCast` signature is `(center, halfExtents, direction, out hit, orientation, distance, mask, qti)`.
  Orientation comes **after** hitInfo.
- Sweep contact is `origin + direction * hit.distance`. Using `hit.point - normal * radius` pushes
  the body into the wall.
- Built players strip shaders nothing references. Create materials as assets under
  `Assets/Resources/Discesa`; `Shader.Find` is not a runtime strategy.
- No custom tags. Resolve objects by component.
- Test assemblies need their own `.asmdef`; an `Editor`-only PlayMode asmdef silently reports
  0 tests while looking green.

## Evidence standard

Do not claim a playtest, visual inspection, mobile test or credit spend that did not happen.
Report the file, the command, and the recorded output. A passing build is not a working game.
