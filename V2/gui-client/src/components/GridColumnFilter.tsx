import { useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import Box from "@mui/material/Box";
import FilterListIcon from "@mui/icons-material/FilterList";
import { DenseButton } from "./DenseField";
import { DENSE_FONT_SIZE } from "../theme/theme";
import { parseDdMmmYy } from "../lib/schedule";

/**
 * Reproduces V1.2's own per-column grid filter — a text box + button under
 * every column header (`Libs/CustomGUIControls/CustomGUIControls/Grid/
 * GridFilter.cs`), the button opening a floating checklist with All/None/
 * OK/Cancel (`GridFilterSelect.cs`), and filters across every column
 * combining with AND (`GridControl.ApplyFiltersToRows`) — see D-Win-14
 * (`Claude/Requirements/UserInterfaceWindows.md`) for the full design
 * discussion, including two deliberate departures from V1.2's own code:
 * the checklist's own value list is scoped to what other columns'
 * currently-active filters leave visible (V1.2's equivalent doesn't
 * actually do this, despite looking like it should — the row-visibility
 * check that would do it is commented out in its source), and selecting
 * "None" then OK here means "show nothing" for that column, not V1.2's own
 * "empty selection quietly disables the filter" quirk.
 */

export type FilterSortType = "string" | "number" | "date";

export interface ColumnFilterState {
  contains: string;
  /** null = no exact-match restriction active (every value passes). */
  exact: Set<string> | null;
}

export const EMPTY_COLUMN_FILTER: ColumnFilterState = { contains: "", exact: null };

// A pale yellow (#fff8d6) was tried first for "this column has an active
// filter" but didn't read as different enough from plain white at a
// glance; this amber has enough saturation to be spotted without being a
// jarring, attention-grabbing colour for something that isn't an error or
// a required action.
const ACTIVE_FILTER_BG = "#ffe082";

export function columnFilterPasses(values: string[], state: ColumnFilterState): boolean {
  if (state.exact) {
    if (!values.some((v) => state.exact!.has(v))) return false;
  }
  const needle = state.contains.trim().toLowerCase();
  if (needle && !values.some((v) => v.toLowerCase().includes(needle))) return false;
  return true;
}

export function isColumnFilterActive(state: ColumnFilterState): boolean {
  return state.exact != null || state.contains.trim() !== "";
}

function sortKey(value: string, sortType: FilterSortType): number | string {
  if (sortType === "number") {
    const n = parseFloat(value);
    return Number.isNaN(n) ? Number.POSITIVE_INFINITY : n;
  }
  if (sortType === "date") {
    return parseDdMmmYy(value) ?? Number.POSITIVE_INFINITY;
  }
  return value.toLowerCase();
}

export function sortFilterOptions(values: string[], sortType: FilterSortType): string[] {
  return [...values].sort((a, b) => {
    const ka = sortKey(a, sortType);
    const kb = sortKey(b, sortType);
    if (typeof ka === "number" && typeof kb === "number") return ka - kb;
    return String(ka).localeCompare(String(kb));
  });
}

function FilterChecklistPopover({
  options,
  initialChecked,
  anchorRect,
  onCancel,
  onOk,
}: {
  options: string[];
  initialChecked: Set<string>;
  anchorRect: DOMRect;
  onCancel: () => void;
  onOk: (checked: Set<string>) => void;
}) {
  const [checked, setChecked] = useState(initialChecked);
  const popoverRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleMouseDown(event: MouseEvent) {
      if (popoverRef.current && !popoverRef.current.contains(event.target as Node)) {
        onCancel();
      }
    }
    // Capture phase, not bubble (the `true` third argument) — every other
    // column's own filter row/button stops propagation on its mousedown
    // (so clicking it doesn't also trigger MUI's own column-header sort
    // click), which silently swallowed this same listener when it was
    // bubble-phase: clicking a second column's filter button never
    // reached this one's document listener at all, so the first popover
    // was left open behind the second one. A capture-phase listener runs
    // before any of those handlers get a chance to stop anything, so it
    // always sees the click regardless (D-Win-16).
    document.addEventListener("mousedown", handleMouseDown, true);
    return () => document.removeEventListener("mousedown", handleMouseDown, true);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function toggle(value: string) {
    setChecked((prev) => {
      const next = new Set(prev);
      if (next.has(value)) next.delete(value);
      else next.add(value);
      return next;
    });
  }

  const top = Math.min(anchorRect.bottom + 2, window.innerHeight - 260);
  // 180 is a floor, not a fixed size: a narrow column (e.g. "T") still
  // gets a usable checklist rather than one squeezed to its own width, but
  // a wide column's popup matches the column — the filter row it opened
  // from — instead of looking arbitrarily narrower than it.
  const width = Math.max(180, anchorRect.width);
  const left = Math.min(anchorRect.left, window.innerWidth - width);

  return createPortal(
    <Box
      ref={popoverRef}
      onMouseDown={(e) => e.stopPropagation()}
      onClick={(e) => e.stopPropagation()}
      sx={{
        position: "fixed",
        top,
        left,
        width,
        maxHeight: 240,
        display: "flex",
        flexDirection: "column",
        bgcolor: "#fff",
        border: "1px solid rgba(0,0,0,0.25)",
        borderRadius: "4px",
        boxShadow: "0 4px 12px rgba(0,0,0,0.22)",
        zIndex: 1300,
        fontSize: DENSE_FONT_SIZE,
      }}
    >
      <Box sx={{ display: "flex", gap: "4px", p: "4px", borderBottom: "1px solid rgba(0,0,0,0.12)" }}>
        <DenseButton onClick={() => setChecked(new Set(options))}>All</DenseButton>
        <DenseButton onClick={() => setChecked(new Set())}>None</DenseButton>
      </Box>
      <Box sx={{ overflowY: "auto", py: "2px" }}>
        {options.length === 0 && (
          <Box sx={{ px: "8px", py: "6px", fontStyle: "italic", color: "rgba(0,0,0,0.5)" }}>
            No values
          </Box>
        )}
        {options.map((option) => (
          <Box
            key={option}
            component="label"
            sx={{
              display: "flex",
              alignItems: "center",
              gap: "6px",
              px: "8px",
              py: "2px",
              cursor: "pointer",
              "&:hover": { bgcolor: "rgba(0,0,0,0.04)" },
            }}
          >
            <input
              type="checkbox"
              checked={checked.has(option)}
              onChange={() => toggle(option)}
              style={{ margin: 0 }}
            />
            <Box
              component="span"
              sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}
            >
              {option === "" ? "(blank)" : option}
            </Box>
          </Box>
        ))}
      </Box>
      <Box sx={{ display: "flex", gap: "4px", p: "4px", borderTop: "1px solid rgba(0,0,0,0.12)" }}>
        <DenseButton variant="filled" onClick={() => onOk(checked)}>
          OK
        </DenseButton>
        <DenseButton onClick={onCancel}>Cancel</DenseButton>
      </Box>
    </Box>,
    document.body,
  );
}

export function FilterableHeader({
  label,
  state,
  onContainsChange,
  onExactChange,
  getOptions,
  columnWidth,
}: {
  label: string;
  state: ColumnFilterState;
  onContainsChange: (text: string) => void;
  onExactChange: (exact: Set<string> | null) => void;
  /** Computed lazily, only when the checklist popover is opened. */
  getOptions: () => string[];
  /** The column's own live (resizable) width in px — see AllTaskOrigPage.tsx's
   * `withFilter` for why this is a number, not "100%". */
  columnWidth: number;
}) {
  const [open, setOpen] = useState(false);
  const [popoverProps, setPopoverProps] = useState<{ options: string[]; anchorRect: DOMRect } | null>(
    null,
  );
  const buttonRef = useRef<HTMLButtonElement>(null);
  // The popover's own left edge lines up with the filter row's left edge
  // (== the text box's own left edge, since the row spans edge-to-edge) —
  // not the button's, which sits at the row's right end — so this ref is
  // on the row itself, not the button.
  const rowRef = useRef<HTMLDivElement>(null);
  const active = isColumnFilterActive(state);

  function openPopover() {
    const options = getOptions();
    const anchorRect = rowRef.current?.getBoundingClientRect();
    if (!anchorRect) return;
    setPopoverProps({ options, anchorRect });
    setOpen(true);
  }

  return (
    <Box
      sx={{ display: "flex", flexDirection: "column", width: columnWidth, gap: "2px", py: "3px" }}
    >
      <Box
        sx={{
          fontSize: DENSE_FONT_SIZE,
          fontWeight: 700,
          overflow: "hidden",
          textOverflow: "ellipsis",
          whiteSpace: "nowrap",
          // The ambient MUI header cell padding is zeroed out grid-wide
          // (AllTaskOrigPage.tsx's own sx) so the filter row below can sit
          // flush with the column's own border lines with no clipping
          // trickery — this restores that same 10px inset for the title
          // text only, which still wants it.
          px: "10px",
        }}
      >
        {label}
      </Box>
      <Box
        ref={rowRef}
        sx={{ display: "flex", gap: 0, height: 20, width: columnWidth }}
        onMouseDown={(e) => e.stopPropagation()}
        onClick={(e) => e.stopPropagation()}
      >
        <Box
          component="input"
          value={state.contains}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onContainsChange(e.target.value)}
          placeholder="filter…"
          sx={{
            flex: 1,
            minWidth: 0,
            height: "100%",
            border: "1px solid rgba(0,0,0,0.25)",
            borderRadius: "2px 0 0 2px",
            fontSize: DENSE_FONT_SIZE,
            fontFamily: "inherit",
            fontWeight: 400,
            px: "3px",
            bgcolor: state.contains.trim() ? ACTIVE_FILTER_BG : "#fff",
          }}
        />
        <Box
          component="button"
          ref={buttonRef}
          onClick={() => (open ? setOpen(false) : openPopover())}
          title="Filter values…"
          sx={{
            width: 20,
            height: "100%",
            flexShrink: 0,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            border: "1px solid rgba(0,0,0,0.25)",
            // Collapses onto the input's own right border rather than
            // doubling it up, so the two read as one joined control with
            // no gap between them, as asked.
            ml: "-1px",
            borderRadius: "0 2px 2px 0",
            cursor: "pointer",
            bgcolor: active ? ACTIVE_FILTER_BG : "#fff",
            color: "rgba(0,0,0,0.7)",
            "&:hover": { bgcolor: "rgba(0,0,0,0.08)" },
          }}
        >
          <FilterListIcon sx={{ fontSize: 13 }} />
        </Box>
      </Box>
      {open && popoverProps && (
        <FilterChecklistPopover
          options={popoverProps.options}
          initialChecked={state.exact ? new Set(state.exact) : new Set(popoverProps.options)}
          anchorRect={popoverProps.anchorRect}
          onCancel={() => setOpen(false)}
          onOk={(checked) => {
            onExactChange(checked.size === popoverProps.options.length ? null : checked);
            setOpen(false);
          }}
        />
      )}
    </Box>
  );
}
