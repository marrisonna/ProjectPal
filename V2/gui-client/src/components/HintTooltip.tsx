import type { ReactElement } from "react";
import Tooltip from "@mui/material/Tooltip";
import { useHintsEnabled } from "../lib/settings";

// D1.4-125 — a pale-yellow, plain-weight look for *every* hint tooltip in
// the app, matching how a native OS/browser tooltip actually looks (MUI's
// own `Tooltip` default — a dark grey/near-black background, white text —
// reads as a much heavier-weight UI element than that, which is exactly
// what was reported). `PlanPage.tsx`'s own custom cursor-following tooltip
// (built before this setting existed, for a reason unrelated to colour —
// see `HintTooltip`'s own doc comment) uses this same colour directly on
// its own `bgcolor`, so all three tooltip mechanisms in the app agree.
export const HINT_TOOLTIP_BG = "#fff9c4";

// D1.4-124 (UserInteractionPlan.md §10) — wraps a single interactive
// element (a plain Box/span/IconButton — anything that can hold a ref, per
// MUI Tooltip's own requirement) with a tooltip naming its available
// gesture(s) and what each does, shown only while the "Hints" setting is
// "On" (`lib/settings.ts`). When Hints is "Off", this renders `children`
// completely unwrapped — not just a hidden/empty Tooltip — so there's no
// stray wrapper element or MUI Tooltip machinery mounted at all for the
// common case once a user has turned hints off.
//
// This is the plain-element half of the app's own hint mechanism —
// `components/DenseDataGrid.tsx`'s own `hint` prop is the equivalent for a
// whole MUI DataGrid (a grid cell has no ref/children of its own to wrap the
// way a plain element does, so that one works by setting a native `title`
// on the grid's own outer container instead). `PlanPage.tsx`'s Gantt view is
// a third case again — it already has its own custom, cursor-following
// tooltip (`BarTooltip`/label tooltip, built before this setting existed,
// specifically because a native tooltip's position can't be offset from the
// cursor) — that one's own hint text is appended directly to its existing
// tooltip content rather than adding a second, competing tooltip on top.
// D1.4-125 — every hint's own text follows one fixed shape, everywhere:
// `"<gesture>: <effect>"` (e.g. `"Click: Open this Project's own window."`),
// one gesture per line (`\n`-joined — `whiteSpace: "pre-line"` below renders
// each as a real line break) when an element has more than one. Enforced by
// convention at each call site, not by this component itself — there's
// nothing here that could validate a caller's own wording.
export function HintTooltip({ hint, children }: { hint: string; children: ReactElement }): ReactElement {
  const hintsEnabled = useHintsEnabled();
  if (!hintsEnabled) return children;
  return (
    <Tooltip
      title={hint}
      arrow
      enterDelay={400}
      placement="top"
      slotProps={{
        tooltip: {
          sx: {
            bgcolor: HINT_TOOLTIP_BG,
            color: "rgba(0,0,0,0.87)",
            fontWeight: 400,
            fontSize: 12,
            border: "1px solid rgba(0,0,0,0.25)",
            whiteSpace: "pre-line",
          },
        },
        arrow: { sx: { color: HINT_TOOLTIP_BG, "&::before": { border: "1px solid rgba(0,0,0,0.25)" } } },
      }}
    >
      {children}
    </Tooltip>
  );
}
