# Reference images via Venice.ai

How we generate concept/turnaround images for Blender LPM recipes. Venice is used only for
**reference art**; geometry is built from primitives by `tools/blender-lpm`, not generated.

Source of truth for parameters: https://docs.venice.ai/api-reference/endpoint/image/generate
Model list and prices: https://docs.venice.ai/models/image.md (checked 2026-11-08, 78 models).

## Endpoint

```
POST https://api.venice.ai/api/v1/image/generate
Authorization: Bearer VENICE_API_KEY
Content-Type: application/json
```

Response returns `images: [base64...]`, or raw bytes when `return_binary: true`.
`variants` 1–4 works only when `return_binary` is false.

## Sizing rules that actually bite

- `width`/`height` are capped at **1280** each and are for pixel models (e.g. `venice-sd35`).
- The **Qwen Image family rejects `width`/`height` with a 400** and needs `aspect_ratio`.
- Resolution-tier models (`gpt-image-2`, `nano-banana-2/pro`) take `aspect_ratio` + `resolution`.
- `transparent_background` only works on models advertising `supportsTransparentBackground`, and
  requires `format` png or webp. Do not assume it; prefer a flat backdrop and key it.

## Model picks for our use

| Need | Model | Why | Cost |
|---|---|---|---|
| Turnaround sheet (primary) | `qwen-image-2-1-turbo` | aspect-ratio model, 2K, cheapest usable | $0.02 (1K/2K) |
| Turnaround sheet (fallback) | `nano-banana-2` | follows multi-view layout instructions best | $0.10 (1K) / $0.14 (2K) |
| Hero 3/4 concept | `krea-3` (medium) | good stylised forms, quality tiers | $0.04 (1K medium) |
| Cheap iteration | `z-image-turbo` / `venice-sd35` | throwaway exploration | $0.01 |

Budget: six images at these prices is **under $0.60**. That is the whole reference budget for the
ship and one prop.

## Brief 1 — ship turnaround sheet (the one that matters)

Four orthographic views in one 2x2 sheet, so proportions are consistent. Crop the quadrants and
feed them to `compare_silhouette.py`.

```json
{
  "model": "qwen-image-2-1-turbo",
  "prompt": "Orthographic model turnaround sheet, 2x2 grid, four views of the same object with identical scale and alignment: top-left front view, top-right left side view, bottom-left back view, bottom-right top view. Object: a compact single-seat six-degree-of-freedom space fighter, 1.3 metres long, 0.7 metres wide, 0.45 metres tall, angular low-poly hard-surface design, wedge nose, flat canopy glass, two short swept wings, twin forward guns, one rear engine nozzle with a visible glow ring, panelled hull. Flat mid-grey background, no shadows on background, even neutral studio lighting, orthographic projection, no perspective, no text, no labels, no watermarks. Clean silhouette, few large flat facets, no surface noise.",
  "negative_prompt": "perspective, foreshortening, dramatic lighting, lens flare, depth of field, text, labels, watermark, multiple objects, cropped object, gradient background, noisy detail, greebles, human figure, cockpit interior",
  "aspect_ratio": "1:1",
  "resolution": "2K",
  "format": "png",
  "seed": 424242,
  "variants": 2,
  "safe_mode": true,
  "hide_watermark": true
}
```

Why these values: `seed` fixed so a re-run compares like for like; two variants so we can pick the
cleaner silhouette without a second paid call; `format: png` for lossless silhouette edges;
`negative_prompt` kills the perspective and text that ruin orthographic comparison.

## Brief 2 — side elevation only (for proportion checks)

```json
{
  "model": "qwen-image-2-1-turbo",
  "prompt": "Strict side elevation of a compact low-poly six-degree-of-freedom space fighter, exactly 1.3 metres long, pointing right, level, no perspective, orthographic, flat mid-grey background, even lighting, clean flat-shaded facets, visible silhouette of nose, canopy, wing, engine nozzle, twin guns. No text, no labels, no measurement lines.",
  "negative_prompt": "perspective, three quarter view, rotating, text, dimensions, arrows, watermark, background clutter, reflections",
  "aspect_ratio": "16:9",
  "resolution": "1K",
  "format": "png",
  "seed": 424242,
  "variants": 1,
  "safe_mode": true
}
```

## Brief 3 — arena prop (industrial crate / pillar)

```json
{
  "model": "krea-3",
  "prompt": "Concept sheet, four orthographic views on a 2x2 grid: a 1.2 metre industrial cargo crate for a mine station interior, weathered steel frame with hazard stripe, recessed handle, bevelled corners, flat-shaded low-poly style with large clean facets. Front, side, back, top. Flat grey background, orthographic, neutral lighting, no text, no logos.",
  "negative_prompt": "perspective, text, logos, rust photo texture, high frequency detail, watermark, character, clutter",
  "aspect_ratio": "1:1",
  "resolution": "1K",
  "quality": "medium",
  "format": "png",
  "seed": 90210,
  "variants": 1,
  "safe_mode": true
}
```

## Pipeline into Blender

1. Save the chosen sheet to `Assets/Art/Source/Blender/ref/<asset>/sheet.png`.
2. Crop quadrants to `front.png`, `side.png`, `back.png`, `top.png`.
3. Write the LPM recipe (parts + palette + budget), run it headless.
4. Compare that view's render with the matching crop:
   `python tools/blender-lpm/scripts/compare_silhouette.py --ref <crop> --render <render>`
5. Gate: IoU >= 0.90 and aspect error <= 3 %, then `gate_report.py` PASS, then export.
6. Record the prompt, model, seed and cost in `docs/PROVENANCE.md`.

## Rules

- Reference images are **inputs**, not shipped assets. They never enter the build.
- One brief per asset; do not regenerate to chase detail that the triangle budget cannot carry.
- Keep the seed. A changed silhouette must come from a changed recipe, not a lucky reroll.
- No API key in the repo, ever. Chris runs the call; the key stays in his environment.
