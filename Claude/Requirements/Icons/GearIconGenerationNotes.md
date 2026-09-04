# Gear Icon — Generation Notes

Practical notes for tweaking the ProjectPal app icon later, without having to rediscover any of this. For the underlying maths (why the tooth shape is what it is), see `GearTeethMaths.md` in this same directory — this document is about the *files and process*, that one is about the *geometry*.

## Where things live

| File | What it is |
|---|---|
| `Claude/Requirements/Icons/generate_gear_icon.py` | The generator. Run it, get the SVG. **This is the actual source of truth** — a set of parameters, not hand-edited path data. |
| `Claude/Requirements/Icons/GearTeethMaths.md` | The derivation: KHK's formulas, the involute curve, two real bugs found and fixed along the way, worked numbers. |
| `V2/gui-client/public/icon.svg` | **The deployed copy** — what the running app actually serves as its favicon and PWA install icon. `gui-client/branding.json`'s `logoPath`/`faviconPath` point at `/icon.svg`; `vite.config.ts`'s PWA manifest config reads the same file (`D1.4-12`). |

(The two other icon concepts considered alongside this one — a "dependency nodes" mark and a monogram badge — along with this file's own once-checked-in copy, were removed from `Claude/Requirements/V1.2_Icons/candidates/` once the gear was chosen and deployed; nothing else referenced that folder.)

There is no build step wiring the generator to the deployed copy — after changing a parameter and re-running the script, copy the output over `V2/gui-client/public/icon.svg` by hand. They were confirmed byte-identical (mod whitespace) when this note was written; if they drift, the generator's output is the one to trust.

## How to tweak something

1. Edit a parameter at the top of `generate_gear_icon.py` (see the table below for what's there).
2. `python generate_gear_icon.py > out.svg` and look at it — open the file in a browser, or embed it in a quick HTML page. The script also prints the derived numbers (module, radii, gap angle) to stderr, worth a glance if you're changing tooth count or size.
3. If it meshes (tooth count or pressure angle changed): sanity-check by placing two copies at centre-distance `2 × r` (pitch radius), one rotated `180°/z`, and confirm the outlines interleave without crossing — `GearTeethMaths.md` §7 has the reasoning. This is exactly the kind of change that *looks* fine and *isn't* without actually checking; see §4 and §5 of that file for two cases where it didn't.
4. Copy the result over `V2/gui-client/public/icon.svg`.

## Parameters (from `generate_gear_icon.py`)

| Name | Current value | Notes |
|---|---|---|
| `Z` | 12 | Tooth count. Changing this changes the module (§below) since `OUTER_RADIUS` is held fixed — re-check proportions look right afterward. |
| `PRESSURE_ANGLE_DEG` | 20 | The universal standard. No reason found to change it. |
| `OUTER_RADIUS` | 100 | Tip radius, in the 256×256 viewBox. Everything else (module, pitch/root/base radii) is derived from this + `Z` + pressure angle — see `GearTeethMaths.md` §2. |
| `HOLE_RADIUS` | 24.5 | Originally 35 (an eyeballed guess); reduced 30% on feedback that it read too large. |
| `RING_INNER_RADIUS` | 30 | Decorative ring around the hole. Not measured off anything — added to echo `RING_OUTER_RADIUS` on request. Free to move. |
| `RING_OUTER_RADIUS` | 63 | Decorative "hub ring" near the tooth root. This one *was* measured off the real `Icons/process.ico` (radial pixel-brightness scan out from the hub, looking for a second dip distinct from the hole's own border) — moving it further from 63 drifts from that source. |
| `N_FLANK_SAMPLES` | 8 | Polyline segments approximating each involute flank. Plenty at this icon's resolution (favicon up to maybe a 512px PWA icon); raise it if the flank ever looks faceted at a larger size. |
| `GRADIENT_STOPS` / `OUTLINE_COLOUR` / `RING_COLOUR` | see script | The chrome shading and stroke colours — see `GearTeethMaths.md` §6 for how the gradient axis was chosen (sampled against the real `.ico`'s pixel brightness at its four compass corners). |

## Things that look like reasonable simplifications but weren't checked against the original

Called out here so a future tweak doesn't accidentally "fix" something that was actually a deliberate call, or waste time re-deriving something already settled:

- **No root fillet.** Below the base circle, the flank is a straight radial line, not a curve. Standard practice when no fillet radius is specified (real manufactured gears round this corner to reduce stress concentration; irrelevant for a flat icon).
- **8-point polyline per involute flank**, not a true parametric curve or a Bézier fit. SVG has no native involute primitive; a plain `L`-per-sample polyline was simplest and looks smooth enough at every size actually tested (16px through 512px).
- **The two decorative rings are circles, full stop** — no attempt to reproduce the original's very subtle shading/bevel on those rings, just a plain stroked circle in a slightly lighter grey than the main outline.
- **Colours are a flat, non-brand grey/chrome palette**, not tied to `branding.json`'s primary/secondary colours. This was a deliberate choice to match the original icon's real silver/chrome look — if the icon should instead go monochrome-brand at some point, that's a bigger visual change than a parameter tweak, worth its own decision rather than folding into this script's defaults.
