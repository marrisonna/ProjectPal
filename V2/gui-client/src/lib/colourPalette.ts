// D-DM-13/D1.4-88 — a small, curated categorical palette (matching the seed
// data's own existing style, a seaborn "muted"/"deep"-style palette) for
// suggesting a new Team member's default Gantt-bar colour: distinct enough
// to tell People apart at a glance, and visually consistent with what's
// already seeded. Once every palette colour is already in use on a given
// Team, `suggestNextColour` falls back to generating further colours via
// the golden-angle hue rotation (each new hue ~137.508° from the last) —
// the standard technique for producing an unbounded sequence of colours
// that stay well-separated from every one before it, without ever
// repeating.
const CATEGORICAL_PALETTE = [
  "#4C72B0",
  "#DD8452",
  "#55A868",
  "#C44E52",
  "#8172B2",
  "#937860",
  "#DA8BC3",
  "#8C8C8C",
  "#CCB974",
  "#64B5CD",
];

function hslToHex(h: number, s: number, l: number): string {
  const a = s * Math.min(l, 1 - l);
  const channel = (n: number) => {
    const k = (n + h / 30) % 12;
    const value = l - a * Math.max(Math.min(k - 3, 9 - k, 1), -1);
    return Math.round(255 * value)
      .toString(16)
      .padStart(2, "0");
  };
  return `#${channel(0)}${channel(8)}${channel(4)}`;
}

// The next colour not already used by anyone on a given Team — the "Add
// Person to Team" dialog's own pre-filled default (still fully editable
// before confirming, never silently forced), so opening the dialog twice in
// a row without confirming suggests the same colour both times, not a
// random one.
export function suggestNextColour(usedColours: (string | null | undefined)[]): string {
  const used = new Set(usedColours.filter((c): c is string => !!c).map((c) => c.toLowerCase()));
  for (const candidate of CATEGORICAL_PALETTE) {
    if (!used.has(candidate.toLowerCase())) return candidate;
  }
  const hue = (used.size * 137.508) % 360;
  return hslToHex(hue, 0.55, 0.5);
}
