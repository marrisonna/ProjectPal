#!/usr/bin/env python3
"""Regenerate the ProjectPal gear icon SVG from parameters.

Implements the involute spur-gear maths documented in GearTeethMaths.md
(this directory) — read that first for *why* the formulas below are what
they are, including two real bugs found and fixed while building this
(a sign error in the flank-angle formula, and a wrong-point-pair bug in the
gap-arc connector). This script exists so a future tweak is "change a
parameter and re-run", not "hand-edit path data" or "ask an AI to redo the
derivation from scratch".

Usage:
    python generate_gear_icon.py > icon.svg

Then copy the output over V2/gui-client/public/icon.svg (the deployed copy —
see GearIconGenerationNotes.md for how that file is wired into the app).
"""

import math

# --- Parameters you might actually want to change -------------------------

Z = 12                  # tooth count
PRESSURE_ANGLE_DEG = 20  # standard; don't change without a reason
OUTER_RADIUS = 100      # target tip radius, in SVG units (drives the module)
HOLE_RADIUS = 24.5      # centre hole
RING_INNER_RADIUS = 30  # decorative ring around the hole
RING_OUTER_RADIUS = 63  # decorative ring near the tooth root ("hub ring")
N_FLANK_SAMPLES = 8     # polyline segments approximating each involute flank

CX, CY = 128, 128        # gear centre, in a 256x256 viewBox
VIEWBOX = "0 0 256 256"

GRADIENT_STOPS = [       # 3-stop linear gradient, axis NE -> SW (see GearTeethMaths.md §6)
    (0, "#9da3aa"),
    (50, "#fbfbfc"),
    (100, "#9da3aa"),
]
OUTLINE_COLOUR = "#5b6472"
OUTLINE_WIDTH = 3
RING_COLOUR = "#8b93a1"
RING_WIDTH = 2

# --- Derived gear dimensions (KHK's module-system formulas) ---------------

alpha = math.radians(PRESSURE_ANGLE_DEG)
m = OUTER_RADIUS / (Z / 2 + 1)      # module, solved from the target tip radius
r = Z * m / 2                        # pitch radius
ha = m                               # addendum
hf = 1.25 * m                        # dedendum
ra = r + ha                          # tip radius (== OUTER_RADIUS, by construction)
rf = r - hf                          # root radius
rb = r * math.cos(alpha)             # base circle radius


def involute_polar_angle(radius: float) -> float:
    """Angle (radians) swept by the involute of the base circle, from its
    own t=0 reference direction, to reach `radius`. radius must be >= rb."""
    t = math.sqrt((radius / rb) ** 2 - 1)
    x = rb * (math.cos(t) + t * math.sin(t))
    y = rb * (math.sin(t) - t * math.cos(t))
    return math.atan2(y, x)


half_tooth_angle_pitch = math.pi / (2 * Z)          # half tooth-thickness angle at the pitch circle
inv_at_pitch = involute_polar_angle(r)
flank_ref_angle_rad = half_tooth_angle_pitch + inv_at_pitch
FLANK_REF_ANGLE_DEG = math.degrees(flank_ref_angle_rad)  # flank angle at rb (and, via the
                                                            # straight radial segment, at rf too)


def flank_angle_deg(radius: float) -> float:
    """Angular position of the RIGHT flank at `radius`, relative to the
    tooth's own centreline. MINUS here is not optional -- see
    GearTeethMaths.md §4's "why minus, not plus" for the failure mode of
    getting this backwards (teeth flare wider outward instead of narrowing)."""
    return FLANK_REF_ANGLE_DEG - math.degrees(involute_polar_angle(radius))


def to_xy(angle_deg: float, radius: float):
    """angle_deg=0 is straight up (north); increasing angle_deg is clockwise."""
    a = math.radians(angle_deg - 90)
    return (round(CX + radius * math.cos(a), 3), round(CY + radius * math.sin(a), 3))


def tooth_points(theta_center_deg: float):
    """One tooth's boundary, LEFT side first, RIGHT side last (this order
    matters -- see GearTeethMaths.md §5's "why left-first, not right-first").
    Ends on the tooth's own right-root point, so consecutive teeth (walked
    in order of increasing theta) connect at the correct pair of points."""
    pts = []
    pts.append(to_xy(theta_center_deg - FLANK_REF_ANGLE_DEG, rf))   # left root
    pts.append(to_xy(theta_center_deg - FLANK_REF_ANGLE_DEG, rb))   # left base
    for i in range(1, N_FLANK_SAMPLES + 1):
        radius = rb + (ra - rb) * i / N_FLANK_SAMPLES
        pts.append(to_xy(theta_center_deg - flank_angle_deg(radius), radius))  # left flank, rb->ra
    pts.append(to_xy(theta_center_deg + flank_angle_deg(ra), ra))   # tip, right side (mirrors last point)
    for i in range(N_FLANK_SAMPLES - 1, -1, -1):
        radius = rb + (ra - rb) * i / N_FLANK_SAMPLES
        pts.append(to_xy(theta_center_deg + flank_angle_deg(radius), radius))  # right flank, ra->rb
    pts.append(to_xy(theta_center_deg + FLANK_REF_ANGLE_DEG, rb))   # right base
    pts.append(to_xy(theta_center_deg + FLANK_REF_ANGLE_DEG, rf))   # right root
    return pts


def build_paths():
    pitch_deg = 360 / Z
    all_teeth = [tooth_points(i * pitch_deg) for i in range(Z)]

    # Fill: one closed path, all teeth + the root-circle gap arcs between
    # them (needed so the boundary is closed), plus the hole (evenodd).
    fill_cmds = [f"M {all_teeth[0][0][0]},{all_teeth[0][0][1]}"]
    for i in range(Z):
        pts = all_teeth[i]
        for (x, y) in pts[1:]:
            fill_cmds.append(f"L {x},{y}")
        nx, ny = all_teeth[(i + 1) % Z][0]
        fill_cmds.append(f"A {rf:.3f},{rf:.3f} 0 0,1 {nx},{ny}")
    fill_d = " ".join(fill_cmds) + " Z"

    hx1, hx2 = CX + HOLE_RADIUS, CX - HOLE_RADIUS
    hole_d = (
        f"M {hx1},{CY} A {HOLE_RADIUS},{HOLE_RADIUS} 0 1,0 {hx2},{CY} "
        f"A {HOLE_RADIUS},{HOLE_RADIUS} 0 1,0 {hx1},{CY} Z"
    )
    fill_d_full = f"{fill_d} {hole_d}"

    # Tooth-outline stroke: twelve independent OPEN subpaths (no arc, no Z)
    # -- deliberately not stroking the root-circle arcs here (see
    # GearTeethMaths.md §5's fill/stroke split explanation).
    tooth_stroke_cmds = []
    for i in range(Z):
        pts = all_teeth[i]
        tooth_stroke_cmds.append(f"M {pts[0][0]},{pts[0][1]}")
        for (x, y) in pts[1:]:
            tooth_stroke_cmds.append(f"L {x},{y}")
    tooth_stroke_d = " ".join(tooth_stroke_cmds)

    # Gap-outline stroke: the same root-circle arcs, but as their own twelve
    # open subpaths, not joined onto either tooth's outline.
    gap_stroke_cmds = []
    for i in range(Z):
        lx, ly = all_teeth[i][-1]
        nx, ny = all_teeth[(i + 1) % Z][0]
        gap_stroke_cmds.append(f"M {lx},{ly} A {rf:.3f},{rf:.3f} 0 0,1 {nx},{ny}")
    gap_stroke_d = " ".join(gap_stroke_cmds)

    return fill_d_full, tooth_stroke_d, gap_stroke_d


def render_svg() -> str:
    fill_d, tooth_stroke_d, gap_stroke_d = build_paths()
    stops = "\n".join(
        f'      <stop offset="{pct}%" stop-color="{colour}"/>' for pct, colour in GRADIENT_STOPS
    )
    return f'''<svg xmlns="http://www.w3.org/2000/svg" viewBox="{VIEWBOX}">
  <defs>
    <linearGradient id="gearChrome" x1="85%" y1="10%" x2="15%" y2="90%">
{stops}
    </linearGradient>
  </defs>
  <path fill-rule="evenodd" fill="url(#gearChrome)" stroke="none" d="{fill_d}"/>
  <path fill="none" stroke="{OUTLINE_COLOUR}" stroke-width="{OUTLINE_WIDTH}" stroke-linejoin="round" stroke-linecap="round" d="{tooth_stroke_d}"/>
  <path fill="none" stroke="{OUTLINE_COLOUR}" stroke-width="{OUTLINE_WIDTH}" d="{gap_stroke_d}"/>
  <circle cx="{CX}" cy="{CY}" r="{HOLE_RADIUS}" fill="none" stroke="{OUTLINE_COLOUR}" stroke-width="{OUTLINE_WIDTH}"/>
  <circle cx="{CX}" cy="{CY}" r="{RING_INNER_RADIUS}" fill="none" stroke="{RING_COLOUR}" stroke-width="{RING_WIDTH}"/>
  <circle cx="{CX}" cy="{CY}" r="{RING_OUTER_RADIUS}" fill="none" stroke="{RING_COLOUR}" stroke-width="{RING_WIDTH}"/>
</svg>
'''


if __name__ == "__main__":
    print(render_svg())
    import sys
    print(
        f"m={m:.4f} r={r:.4f} ra={ra:.4f} rf={rf:.4f} rb={rb:.4f} "
        f"flank_ref_angle={FLANK_REF_ANGLE_DEG:.4f} deg "
        f"gap_angle={360/Z - 2*FLANK_REF_ANGLE_DEG:.4f} deg",
        file=sys.stderr,
    )
