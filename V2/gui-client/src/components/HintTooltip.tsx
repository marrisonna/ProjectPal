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
export function HintTooltip({
  hint,
  children,
  enterDelay = 400,
}: {
  hint: string;
  children: ReactElement;
  // Stage5 — 400ms everywhere by default (a deliberate hover-intent delay,
  // avoiding a "tooltip storm" while the mouse crosses several hintable
  // elements quickly), but overridable to `0` for a caller sitting directly
  // beside `DenseDataGrid.tsx`'s own instant, delay-free `gridHintTooltip`
  // (the filter row's label/input/icon, `GridColumnFilter.tsx`; the "View
  // Gantt" button, `DenseField.tsx`'s `DenseButton`) — reported as a
  // noticeable, inconsistent delay right next to a tooltip with none at all.
  enterDelay?: number;
}): ReactElement {
  const hintsEnabled = useHintsEnabled();
  if (!hintsEnabled) return children;
  return (
    <Tooltip
      title={hint}
      arrow
      enterDelay={enterDelay}
      placement="top"
      // Stage5 — MUI's own default (`disableInteractive={false}`) keeps the
      // popper itself hoverable (`pointer-events: auto`), meant for a
      // tooltip whose own content needs to be hovered (e.g. a link inside
      // it) — none of this app's hints have that. Left at the default, a
      // "top"-placed tooltip on one element can render directly over a
      // *different* element positioned just above it (`GridColumnFilter.
      // tsx`'s filter row sits directly under its own sortable label), and
      // — since the popper intercepts the pointer — moving the mouse
      // upward into that other element actually enters the tooltip popup
      // first, never reaching the real element underneath: reported as
      // "moving the mouse up through the filter/sort header, the sort
      // header's own tooltip never appears and the filter's just closes."
      // `disableInteractive` makes the popper `pointer-events: none`,
      // letting the cursor pass straight through to whatever's really
      // there, regardless of which direction it's moving.
      disableInteractive
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
