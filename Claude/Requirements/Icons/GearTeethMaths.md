# Gear Teeth Maths — the ProjectPal app icon

How the tooth shape in the redrawn app-icon gear is actually constructed, so it can be reproduced from this document alone, without the git history that got here. Source of truth for the shape is `generate_gear_icon.py` in this directory; the deployed copy is `V2/gui-client/public/icon.svg` — see `GearIconGenerationNotes.md` in this directory for how those relate and what to do to change either.

**Source:** [KHK's spur gear dimension calculations](https://khkgears.net/new/gear_knowledge/gear_technical_reference/calculation_gear_dimensions.html) — the standard module-system formulas below (addendum, dedendum, base circle) are theirs; the icon-specific choices (tooth count, target size, SVG angle convention) are ours.

The tooth is a real involute profile, not a straight-sided approximation — that distinction is the entire point of this document. An earlier pass used straight radial-edged teeth (easy to get "close enough" by eye, wrong under any real test — two straight-sided gears rotated to mesh don't actually interlock without collision). Everything here follows from the standard formulas instead.

## 1. Choose the gear parameters

Two independent choices drive everything else:

| Symbol | Meaning | This icon's value |
|---|---|---|
| `z` | number of teeth | **12** |
| `α` | pressure angle | **20°** (the universal standard; don't change this without a reason) |
| `ra` | target tip (outer) radius, in SVG units | **100** (chosen to match the rest of the icon: viewBox `0 0 256 256`, centre `(128,128)`, hole radius 24.5, decorative rings at radius 30 and 63 — §6) |

Everything below is *derived* from `z`, `α`, and `ra` — there is no free parameter left over.

## 2. Standard module-system formulas (KHK)

The **module** `m` is the fundamental unit gear dimensions are expressed in. Given a *target* tip radius rather than a starting module, invert the tip-radius formula to solve for `m` first:

```
da = m(z + 2)              tip (outer) diameter           →  m = 2·ra / (z + 2)
d  = z·m                   reference (pitch) diameter      →  r = z·m / 2
ha = 1.00·m                addendum (tip height above pitch circle)
hf = 1.25·m                dedendum (root depth below pitch circle)
ra = r + ha                tip radius        (= the target we started from, by construction)
rf = r − hf                root radius
rb = r·cos(α)               base circle radius — where the involute curve starts
```

**Worked values for this icon** (`z=12`, `α=20°`, `ra=100`):

```
m  = 14.285714
r  = 85.714286   (pitch radius)
ha = 14.285714
hf = 17.857143
ra = 100.000000  (tip radius — matches the target, confirming the algebra)
rf = 67.857143   (root radius)
rb = 80.545082   (base radius)
```

Note `rb > rf` here (80.5 > 67.9) — with only 12 teeth the base circle sits *outside* the root circle. That's normal for low tooth counts and matters for step 4 below: part of each flank, between the root and the base circle, is **not** part of the involute at all.

## 3. The involute curve itself

An involute is the path traced by the end of a taut string unwound from the base circle. Parametrised by the "roll angle" `t` (radians, `t ≥ 0`, `t=0` at the base circle):

```
x(t) = rb·(cos t + t·sin t)
y(t) = rb·(sin t − t·cos t)
```

This is in the involute's *own* local frame: `t=0` sits at `(rb, 0)`, and increasing `t` spirals outward and around. Two derived quantities matter:

- **Radius at parameter `t`:** `R(t) = rb·√(1+t²)` — invert this to find `t` for a target radius: `t(R) = √((R/rb)² − 1)`.
- **Polar angle swept from the local `t=0` line**, i.e. `atan2(y(t), x(t))` — call this `sweep(R)`. It has a closed form too, the *involute function* `inv(β) = tan(β) − β` where `β = arccos(rb/R)` is the pressure angle *at* radius `R`, but computing it via `atan2` on `x(t),y(t)` directly is simpler to implement correctly and is what was actually used here.

## 4. Positioning one flank

The pressure angle `α` is *defined* at the pitch circle, so the pitch-circle sweep is just `inv(α)`:

```
half_tooth_angle_pitch = π / (2z)                     — half the tooth's angular thickness at the pitch
                                                          circle, standard (no profile-shift) gears: the
                                                          tooth occupies exactly half of one pitch angle
inv_at_pitch            = tan(α) − α                    — sweep(r), evaluated at the pitch radius

flank_ref_angle          = half_tooth_angle_pitch + inv_at_pitch
                                                        — the flank's angular position AT THE BASE CIRCLE,
                                                          relative to the tooth's own centreline
```

Then for any radius `R` between `rb` and `ra`, the flank's angular position relative to the tooth centreline is:

```
flank_angle(R) = flank_ref_angle − sweep(R)
```

**The minus sign is not optional and is easy to get backwards** (ask why below). Get it wrong and the teeth flare wider going outward instead of narrowing — looks *plausible* at a glance, meshes with nothing.

<details>
<summary>Why minus, not plus (the mistake made and fixed while building this)</summary>

Physically, a real gear tooth is widest at the root and narrows toward the tip. Algebraically: the standard tooth-thickness-at-radius formula is `half_angle(R) = half_angle(pitch) + inv(α_pitch) − inv(α_R)`, and `inv(α_R)` grows quickly as `R` grows past the pitch radius (pressure angle `α_R` climbs toward 90°). So the correction term is negative for `R > r`, and `flank_angle(R)` must *decrease* as `R` increases. Writing `flank_ref_angle + sweep(R)` instead produces the opposite — a tooth that gets wider outward, which is wrong. Sanity check after implementing: confirm `flank_angle(ra) < flank_angle(rb)`. For this icon: `8.354° at rb` down to `2.541° at ra` — narrowing, correct.
</details>

The **left flank** is the mirror image: `−flank_angle(R)`.

**Below the base circle** (from `rf` up to `rb`), there is no involute — this icon uses a plain straight radial line at the constant angle `flank_ref_angle` (i.e. the flank's angle *at* the base circle, held constant down to the root). This is the standard simplification when no root fillet radius is specified; a manufactured gear would round this corner, but for an icon it doesn't matter.

## 5. Assembling one tooth, then the whole gear

Per tooth, centred at absolute angle `θ = i·(360°/z)` for `i = 0..z−1`, walk these points in order — **left side first, right side last**:

1. Root, left flank: `(θ − flank_ref_angle, rf)`
2. Base, left flank: `(θ − flank_ref_angle, rb)`
3. Involute samples, left flank, `rb → ra` (mirrored, i.e. negate the angle): `(θ − flank_angle(R), R)` for `R` stepping from `rb` to `ra` (8 steps was plenty at this icon's resolution — a straight polyline through the sampled points, since SVG has no native involute primitive)
4. Tip, right flank (mirrors step 3's last point): `(θ + flank_angle(ra), ra)`
5. Involute samples, right flank, `ra → rb`: reverse of step 3, unmirrored
6. Base, right flank: `(θ + flank_ref_angle, rb)`
7. Root, right flank: `(θ + flank_ref_angle, rf)`

**The left-first order matters and is not arbitrary** — see the warning below.

Then connect tooth `i`'s step-7 point to tooth `i+1`'s step-1 point with a **circular arc of radius `rf`** (SVG `A rf,rf 0 0,1 x,y`) — this is the gap floor, and it's a true arc (not a polyline chord) since it's genuinely a section of the root circle. This closed, all-12-teeth-plus-gap-arcs path is used for the **fill** — it has to include the gap arcs, or the fill boundary isn't closed.

<details>
<summary>Why left-first, not right-first (a second mistake made and fixed while building this)</summary>

An earlier version of this file walked each tooth **right**-first (right-root → tip → left-root), which draws the identical *tooth shape* — but breaks the connecting arc above. With right-first ordering, tooth `i`'s last point is its own *left*-root (the trailing/counter-clockwise edge, adjacent to the *previous* tooth, `i−1`), while tooth `i+1`'s first point is *its* right-root (adjacent to tooth `i+2`). Joining those two points with the "minor arc" flag doesn't draw the ~13° gap between tooth `i` and tooth `i+1` at all — it draws a ~47° arc that runs straight underneath the entirety of tooth `i+1`, because the two points it's actually connecting are on opposite sides of that tooth, not the two edges of one gap. This was invisible in the **fill** (an evenodd-filled closed shape still comes out looking right even when one internal arc segment routes strangely, since the tooth's own straight-line edges are what visually define the boundary there) — but became obvious as soon as the same arc was drawn as a **stroke**: a dark line running under the tooth, not confined to the notch. Sanity check after implementing: the angular span of the connecting arc should equal `pitch − 2×flank_ref_angle` (≈ 13.29° for this icon) — verify this on the two actual endpoints before trusting the render.
</details>

**The stroke is built from two separate paths, not one**, both `fill="none"` and both the same colour/width, but kept structurally apart:

- **Tooth outline** — steps 1–7 only, repeated per tooth as twelve independent open subpaths (`M` at step 1, `L` through the rest, stopping at step 7 — no arc, no `Z`).
- **Gap outline** — the same root-circle arcs the fill path uses (`A rf,rf 0 0,1 x,y`, from tooth `i`'s step-7 point to tooth `i+1`'s step-1 point), but as their own twelve open subpaths, not joined onto either tooth's outline.

Splitting it this way is deliberate, not incidental: the **fill** (one path, teeth + gap arcs + hole, closed) is what makes the colour run continuously from a tooth into the gear body — that only needs the shape to be closed, not stroked. The **stroke** then adds the outline back on top, in two pieces, so the gap between two teeth still reads as a real cut edge (matching the original) without any extra stroke weight landing on the teeth's own straight base edges, which already have their own outline from the first piece. Rendering the whole boundary — teeth and gaps — as a single stroked path instead produces the same picture in principle, but it's what an earlier pass here actually did, and it visibly read as a single heavy ring right at the tooth base; splitting the *stroke* (while keeping the *fill* as one path throughout) is what fixed that.

### SVG angle convention used

`angle_deg = 0` points straight up (north) from the gear's centre; increasing `angle_deg` goes clockwise. Converting an `(angle_deg, R)` pair to SVG coordinates, with the gear centred at `(cx, cy) = (128, 128)`:

```
a = radians(angle_deg − 90)
x = cx + R·cos(a)
y = cy + R·sin(a)
```

(Any consistent convention works — this is just the one the actual path data in `gear-recreation.svg` uses, so it's what lets you check your own output against it point-for-point.)

## 6. Everything that isn't the tooth shape

Not covered by KHK's formulas, but needed to reproduce the actual icon file:

- **Centre hole:** plain circle, radius **24.5** (originally 35, reduced 30% on feedback that it read too large against the tooth proportions), centred `(128,128)`. Combined with the gear outline in the fill path via `fill-rule="evenodd"`, and given its own separate stroked `<circle>` (same radius, `stroke="#5b6472" stroke-width="3"`) so its rim stays outlined even though the main gear stroke (§5) no longer traces circular arcs.
- **Decorative hub ring:** the original `process.ico` has a fine groove/collar between the hole and the tooth root, at radius **63** — a plain stroked circle (`fill="none"`, `stroke-width="2"`), not derived from the gear formulas at all, just measured off the original by sampling pixel brightness radially outward from the hub until a second dip (distinct from the hole's own border) turned up.
- **Ring around the hole:** a second, smaller decorative ring at radius **30**, styled identically to the hub ring above (same stroke colour/width) — added to echo it on the other side of the hole, on request; not measured off the original, no formula behind the specific radius beyond "close around the hole," similar in spirit to how the hub ring sits close to the root.
- **Chrome shading:** a 3-stop linear gradient, axis running **NE → SW** (`x1="85%" y1="10%" x2="15%" y2="90%"`), stops `#9da3aa` (0%) → `#fbfbfc` (50%) → `#9da3aa` (100%). This gives a light diagonal band running NW→SE *through the centre* (since NW/SE points sit near the gradient's light midpoint at any radius, while only the NE/SW extremes reach the dark ends) — matches the original's highlight direction, confirmed by sampling actual pixel brightness at the four compass corners of the source `.ico`.
- **Outline stroke:** `#5b6472`, width 3 — on both stroke paths from §5 (tooth outline and gap outline), not the fill path (the fill path has `stroke="none"`). The tooth-outline path additionally sets `stroke-linejoin="round"` and `stroke-linecap="round"`, for the vertices along each tooth's own profile. Hole rim uses the same colour/width as its own circle. Both decorative rings (§6) use a lighter `#8b93a1`, width 2.

## 7. Verifying it actually meshes

Two identical involute gears with pitch radius `r` mesh correctly when:

```
centre distance = r₁ + r₂ = 2r      (their pitch circles tangent — the standard spur-gear meshing distance)
```

For this icon: `2 × 85.714286 = 171.428571`. Place a second copy of the gear at that distance, rotated by half a tooth pitch (`180°/z` = **15°** for `z=12`) so a tooth on one meets a gap on the other. If the flank math above is right, the two outlines interleave with the curves never crossing — that's the actual test, not just "do the numbers look plausible."
