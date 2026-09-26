import { useEffect, useRef, useState } from "react";
import {
  DataGrid,
  useGridApiRef,
  type GridCellParams,
  type GridColDef,
  type GridColumnVisibilityModel,
  type GridRenderEditCellParams,
  type GridSingleSelectColDef,
  type GridSortModel,
  type ValueOptions,
} from "@mui/x-data-grid";
import type { SxProps, Theme } from "@mui/material/styles";
import Box from "@mui/material/Box";
import Snackbar from "@mui/material/Snackbar";
import Alert from "@mui/material/Alert";
import Menu from "@mui/material/Menu";
import MenuItem from "@mui/material/MenuItem";
import ListItemText from "@mui/material/ListItemText";
import { branding, DENSE_FONT_SIZE } from "../theme/theme";
import { formatApiError } from "../lib/apiErrors";
import {
  columnFilterPasses,
  EMPTY_COLUMN_FILTER,
  FilterableHeader,
  sortFilterOptions,
  type ColumnFilterState,
  type FilterSortType,
} from "./GridColumnFilter";

// The dense-grid chrome shared by every grid in this app (D1.4-73,
// `SearchPlan.md` §5) — sizing, borders, header styling, text measurement/
// auto-sizing, and the right-click context menu, extracted from
// `features/tasks/TaskGrid.tsx` once Search needed a second, differently
// shaped grid consumer for it. What stays out on purpose: column
// definitions, cell editability, and any entity-specific concept (Task's
// own Urgency tint) — those differ enough between consumers that forcing
// them through one shared, parameterised path risks exactly the kind of
// cross-entity bug `D1.4-64`/`D1.4-70` already found once elsewhere in this
// app. A consumer with few, fixed, non-editable columns (Search's own
// results grid) can use just `DenseDataGrid` itself, skipping
// `useDenseGridColumns` entirely.

// Dense, WinForms-like row/header sizing (D-Win-11/14) — these are the
// exact literal pixel values wanted; `density="compact"` is deliberately
// never used, since DataGrid multiplies whatever rowHeight/
// columnHeaderHeight is *given* by a further density factor on top.
export const DENSE_ROW_HEIGHT = 22;
// Tight to FilterableHeader's own content now that its own py/gap are both
// 0 (GridColumnFilter.tsx) — the label line plus the filter row's own
// fixed 20px height. A structural fix (making that content stretch to
// fill columnHeaderHeight exactly, rather than tuning this constant to
// match it) was tried and made things worse — DataGrid's own header cell
// sizing doesn't behave as a simple, predictable flex container once
// fought with height/alignSelf overrides — so this stays a plain, tuned
// pixel value instead, shaved down further from 40 since a small amount
// of MUI's own default vertical centring was still visible either side of
// the content at that value.
export const HEADER_HEIGHT = 36;
// With the filter row hidden (D1.4-56's "Show Filter"/"Hide filter" toggle,
// or a grid with no filtering at all — Search's own results grid), the
// header only needs to fit FilterableHeader's own label line — freeing the
// rest of HEADER_HEIGHT back to data rows.
export const HEADER_HEIGHT_NO_FILTER_ROW = 16;

// FilterableHeader's own label padding (GridColumnFilter.tsx: `px: "10px"`
// on the label Box, i.e. 10px each side = 20px) plus a real safety margin —
// not just rounding tolerance: CSS `text-overflow: ellipsis` doesn't clip
// by however many pixels are actually missing, it drops *whole trailing
// characters* until what's left plus "…" fits, so even a few px of
// genuine shortfall reads as several missing characters. Better to have a
// little unused space than to trigger that.
export const HEADER_LABEL_PADDING = 32;
// MUI DataGrid's own default cell horizontal padding is `0 10px`
// (GridRootStyles.js: `.MuiDataGrid-cell/-columnHeader { padding: '0
// 10px' }`, confirmed by reading it directly) — 20px — plus the same
// ellipsis safety margin as above.
export const CELL_PADDING = 30;
// A boolean column ("T") renders a checkbox icon, not text — not
// meaningfully measurable via canvas the way every other column's actual
// displayed text is, so this is a plain fixed width instead (a bit more
// than DENSE_ROW_HEIGHT's own 22px, since the icon needs a little
// breathing room on each side).
export const BOOLEAN_COLUMN_WIDTH = 30;

export const HEADER_FONT_WEIGHT = 700;
export const CELL_FONT_WEIGHT = 400;
// Shorthand strings kept only for the Font Loading API (`document.fonts
// .check`/`.load`), which requires exactly this CSS font shorthand syntax.
export const HEADER_FONT = `${HEADER_FONT_WEIGHT} ${DENSE_FONT_SIZE}px ${branding.fontFamily}`;
export const CELL_FONT = `${CELL_FONT_WEIGHT} ${DENSE_FONT_SIZE}px ${branding.fontFamily}`;

// Every column is sized to its own heading/content by measuring real text
// against the grid's own font via an offscreen canvas, computed directly in
// `withFilter` (`useDenseGridColumns`, below) — *not* MUI DataGrid's own
// built-in `autosizeColumns`. That was tried first and abandoned: `columns`
// necessarily gets rebuilt (a new array/object graph) on every render,
// since its column defs close over live `filterState`/etc., and DataGrid
// re-derives each column's width straight from its own colDef
// (`hydrateColumnsWidth`, gridColumnsUtils.js) every time that `columns`
// prop's identity changes — falling back to a flat 100px for any column
// with no explicit `width` of its own. `autosizeColumns` is also async (at
// least one Promise tick), so even re-running it on every render couldn't
// reliably win that race before the *next* incidental re-render (a filter
// keystroke, an unrelated prop from the parent, anything) rebuilt `columns`
// again and reset it. Computing an explicit `width` ourselves,
// synchronously, every time a column def is built sidesteps the whole
// problem: a rebuild can only ever arrive at the same correct answer, never
// a wrong default.
// A hidden, offscreen <span> — not a canvas 2D context — reused for every
// measurement. Canvas measureText was tried first and, even once Inter was
// confirmed loaded (document.fonts.check), still produced widths narrower
// than several strings actually render at, badly enough to truncate them:
// a canvas created via document.createElement but never attached to the
// document doesn't reliably go through the same font-resolution/text-
// shaping path a real, live DOM element does, so a font it should have
// available can still measure using a substituted fallback. An actual DOM
// element, appended to the page, goes through exactly the same layout and
// font engine the grid's own cells do — there's no plausible way for it to
// disagree with them, since they're both just ordinary DOM/CSS text.
// Individual longhand style properties, not the `font` shorthand — that
// was tried first and is likely the actual reason truncation persisted
// even after switching from canvas to a real DOM element: `element.style
// .font = "..."` goes through the CSS shorthand parser (stricter than
// canvas's own lenient `ctx.font`), and an invalid/unrecognised shorthand
// value is *silently rejected*, leaving the element at whatever font it
// already had — here, no explicit font at all, i.e. the document's
// default serif/sans-serif at its own default size, which happens to
// measure narrower than Inter at 12px for exactly the strings that were
// truncating and wide enough not to visibly matter for the others. Each
// longhand property below has simple, unambiguous parsing, with nothing
// to silently reject.
let measurementSpan: HTMLSpanElement | null = null;
export function measureTextWidth(text: string, fontWeight: number): number {
  if (!measurementSpan) {
    measurementSpan = document.createElement("span");
    measurementSpan.style.position = "absolute";
    measurementSpan.style.visibility = "hidden";
    measurementSpan.style.whiteSpace = "pre";
    measurementSpan.style.top = "-9999px";
    measurementSpan.style.left = "-9999px";
    measurementSpan.style.fontFamily = branding.fontFamily;
    measurementSpan.style.fontSize = `${DENSE_FONT_SIZE}px`;
    document.body.appendChild(measurementSpan);
  }
  measurementSpan.style.fontWeight = String(fontWeight);
  measurementSpan.textContent = text;
  return measurementSpan.getBoundingClientRect().width;
}

// Whether Inter (main.tsx's @fontsource/inter imports) has actually
// finished loading — its real font files are fetched over the network,
// asynchronously, so measureTextWidth calls made before this resolves are
// silently measuring against whatever fallback font the browser
// substitutes in the meantime (system-ui/sans-serif), which is narrower
// than Inter for several strings — the actual cause of a real, reported
// bug: specific columns truncating their own content on a typical page
// load, not just some rare first-millisecond flash. `document.fonts.load`
// both starts the fetch (if it hasn't already, e.g. a very first cold
// load) and resolves once it's ready; `document.fonts.check` covers the
// far more common case where it's already loaded (every earlier screen
// already rendered Inter text) by the time this component first mounts, so
// there's no needless render delay/flash then.
export function useDenseFontsReady(): boolean {
  const [fontsReady, setFontsReady] = useState(
    () => document.fonts.check(HEADER_FONT) && document.fonts.check(CELL_FONT),
  );
  useEffect(() => {
    if (fontsReady) return;
    let cancelled = false;
    Promise.all([document.fonts.load(HEADER_FONT), document.fonts.load(CELL_FONT)]).then(() => {
      if (!cancelled) setFontsReady(true);
    });
    return () => {
      cancelled = true;
    };
  }, [fontsReady]);
  return fontsReady;
}

function optionValue(option: ValueOptions): string | number | null {
  return typeof option === "object" ? option.value : option;
}
function optionLabel(option: ValueOptions): string {
  return typeof option === "object" ? option.label : String(option);
}

// A plain native <select>, not MUI's own Select/Menu — the same "native
// controls, sized explicitly" convention DenseField.tsx already established
// for this whole app (Q1.4-17), and for the same reason here: MUI's own
// singleSelect edit cell (GridEditSingleSelectCell) renders its dropdown
// through a themed Popper portalled straight onto <body>, entirely outside
// the grid's own DOM subtree — so its font size falls back to the ambient
// MUI theme default (16px/body1) rather than inheriting the grid's own
// dense font, however the grid's own sx is set. A native <select> has no
// such portal: the browser always renders its dropdown using the trigger
// element's own computed font, so setting fontSize here is guaranteed to
// apply to the closed cell, the open dropdown, and every option in it.
//
// Also commits immediately on selection (setEditCellValue then
// stopCellEditMode together, not left to a later blur) — MUI's own default
// only calls setEditCellValue on change and waits for the cell to lose
// focus to actually commit, which is what made a value picked here not
// show up in an already-open detail window until the user clicked
// elsewhere in the grid first.
export function DenseSingleSelectEditCell<T>(props: GridRenderEditCellParams<T>) {
  const { id, field, value, colDef, row, api } = props;
  // `api` (above) is the grid's own live GridApiCommunity, passed straight
  // in via GridRenderEditCellParams — not `useGridApiRef()`, which called
  // fresh here would just be a brand-new, disconnected `useRef(null)`
  // (confirmed by reading MUI's own source, DenseDataGrid.tsx's own comment
  // on the sort-history effect below) never attached to any `<DataGrid
  // apiRef={...}>`, so `.current` would stay `null` forever and every
  // selection here would silently throw inside this onChange handler
  // instead of ever committing.
  // colDef here is the grid's own runtime GridStateColDef, which doesn't
  // carry the singleSelect-specific valueOptions in its type even though
  // it's present at runtime (this cell only ever renders for a column that
  // set type: "singleSelect") — cast to the real declared shape rather than
  // widen the whole function to `any`.
  const singleSelectColDef = colDef as unknown as GridSingleSelectColDef<T>;
  const options: ValueOptions[] =
    typeof singleSelectColDef.valueOptions === "function"
      ? singleSelectColDef.valueOptions({ id, row, field })
      : (singleSelectColDef.valueOptions ?? []);

  return (
    <select
      autoFocus
      value={value == null ? "" : String(value)}
      style={{
        width: "100%",
        height: "100%",
        fontSize: DENSE_FONT_SIZE,
        fontFamily: "inherit",
        border: "none",
        outline: "none",
        background: "transparent",
        padding: "0 5px",
      }}
      onChange={async (event) => {
        const raw = event.target.value;
        const matched = options.find((o) => String(optionValue(o) ?? "") === raw);
        const newValue = matched ? optionValue(matched) : raw;
        await api.setEditCellValue({ id, field, value: newValue });
        api.stopCellEditMode({ id, field });
      }}
    >
      {options.map((option) => {
        const v = optionValue(option);
        return (
          <option key={String(v)} value={v == null ? "" : String(v)}>
            {optionLabel(option)}
          </option>
        );
      })}
    </select>
  );
}

interface FilterConfig<T> {
  getValues: (row: T) => string[];
  sortType: FilterSortType;
  // D1.4-117 — the same label `withFilter`'s own `headerMinWidth` measures
  // against, kept here too so `fitColumnsToContent` (below) can recompute
  // that exact formula for a column without needing its own `GridColDef`
  // (long gone by the time an effect calls it after mount) — just this
  // registry, already populated by the time any column-content-width
  // question gets asked.
  headerLabel: string;
}

// A boolean column's "isBoolean" case is a plain fixed width (not
// meaningfully measurable text, see BOOLEAN_COLUMN_WIDTH above); every
// other column measures the widest actual cell content across every row —
// `getValues` (the same function each column already passes to build its
// own filter-dropdown options) is the source of truth for "what does this
// cell display," not a separately-derived `valueGetter`-based version (that
// was tried first and is wrong: it matches the *stored* value, not
// necessarily what's *shown* — a singleSelect column resolving a raw id to
// a display name being the concrete case that broke it). Joined with ", "
// for a multi-value column to reconstruct the same joined string its own
// `valueGetter` actually renders.
function measureColumnContentWidth<T>(
  getValues: (row: T) => string[],
  rows: T[],
  isBoolean: boolean,
): number {
  if (isBoolean) return BOOLEAN_COLUMN_WIDTH;
  let max = 0;
  for (const row of rows) {
    const text = getValues(row).join(", ");
    if (!text) continue;
    const width = measureTextWidth(text, CELL_FONT_WEIGHT);
    if (width > max) max = width;
  }
  return Math.ceil(max) + CELL_PADDING;
}

export interface UseDenseGridColumnsResult<T> {
  // Registers `col` as filterable/auto-sized: `getValues` backs both the
  // filter-dropdown options and the auto-width measurement (see above);
  // `widthCap`, if given, is this column's own maximum default width (a
  // consumer's concern — e.g. TaskGrid's Description/Detailed Description/
  // Ref URL columns); `flex`, if given, makes this the one column that
  // grows/shrinks to fill the grid's remaining width as the window
  // resizes (e.g. Search's own Description column) instead of a fixed
  // pixel width. Both `widthCap` and `flex` stop applying the moment the
  // user manually resizes the column — from then on its own choice wins
  // verbatim, see `withFilter`'s own body.
  withFilter: (
    col: GridColDef<T>,
    getValues: (row: T) => string[],
    sortType: FilterSortType,
    widthCap?: number,
    flex?: number,
  ) => GridColDef<T>;
  // A function, not a plain value — deliberately. `passesAllFilters`
  // (internal) reads `filterConfigs`, which is only *populated* as the
  // caller calls `withFilter` once per column, all of which happens
  // *after* this hook has already returned (building its own `columns`
  // array is the caller's own, separate code, e.g. TaskGrid.tsx's
  // `allColumnDefs`). Computing the filtered rows eagerly here, before any
  // `withFilter` call had happened, meant every filter silently did
  // nothing at all — a real, shipped regression from this hook's own
  // extraction, only caught once someone actually typed into a filter box
  // and watched nothing happen. Call this only once every column for this
  // render has been built (immediately before rendering `DenseDataGrid`,
  // never any earlier), so `filterConfigs` is complete by the time it
  // actually reads from it.
  getFilteredRows: () => T[];
  // D1.4-117 — sets each of `fields`' own manual width (the same path a
  // drag-resize or the header-label double-click records into) to fit its
  // widest content among whichever rows currently pass *every* active
  // filter — exactly `withFilter`'s own double-click-to-fit gesture, just
  // triggered programmatically for a caller-chosen set of fields instead of
  // by the user clicking a specific label. Meant to be called once, from an
  // effect gated to run only on mount (e.g. `useEffect(() =>
  // fitColumnsToContent([...]), [])`) — same "only safe once every
  // `withFilter` call for this render has already happened" timing
  // constraint as `getFilteredRows` above, satisfied automatically by an
  // effect (which always runs after the full render commits) regardless of
  // where in the component it's written. A field not registered via
  // `withFilter` (not a column this grid actually has) is silently skipped,
  // not an error — so a caller can safely name fields another embedding of
  // the same grid might not show.
  fitColumnsToContent: (fields: string[]) => void;
  filterVisible: boolean;
  setFilterVisible: (visible: boolean | ((prev: boolean) => boolean)) => void;
  resetFilters: () => void;
  // Wire straight into `DenseDataGrid`'s own `onColumnResize` prop — records
  // a user's manual column-width choice into this hook's own internal
  // manual-widths state (not exposed directly) so a later `withFilter` call
  // uses it verbatim instead of recomputing a default width.
  onColumnResize: (field: string, width: number) => void;
}

// The shared filtering/auto-sizing subsystem behind every `withFilter`-built
// column — generic over row type `T`. A consumer with only a few, fixed,
// non-editable columns (Search's own results grid, `SearchPlan.md` §4.4)
// doesn't need this at all and can build plain `GridColDef`s directly,
// passing them straight to `DenseDataGrid` below.
export function useDenseGridColumns<T>({
  rows,
  getRowId,
  initialFilterState,
  showFilters = true,
  apiRef,
}: {
  rows: T[];
  // The same row-identity function every caller already passes to
  // `DenseDataGrid`'s own `getRowId` prop — needed here too, for the
  // per-row width cache below (`columnWidthCacheRef`), to tell "this row's
  // value actually changed" from "this is the same row as last render."
  getRowId: (row: T) => string | number;
  initialFilterState?: Record<string, ColumnFilterState>;
  showFilters?: boolean;
  // Optional — only needed for the double-click sort-preservation feature
  // below (D1.4-79). The caller creates it (`useGridApiRef()`) and must
  // pass the *same* instance to `DenseDataGrid`'s own `apiRef` prop, since
  // this hook subscribes to that grid's own event bus directly rather than
  // going through any prop `DenseDataGrid` itself exposes.
  apiRef?: ReturnType<typeof useGridApiRef>;
}): UseDenseGridColumnsResult<T> {
  // Per-column cache of each row's own measured content width, keyed by row
  // id (D1.4-89/ManagePeoplePlan.md's own perf finding): editing a single
  // cell in a grid built on this hook previously froze the tab solid for
  // several seconds, because `withFilter` re-measures *every* row's content
  // on *every* render, and `measureTextWidth` (above) forces a real,
  // synchronous browser layout reflow per call — cheap once, not cheap
  // repeated for every row on every keystroke/edit. A row whose value is
  // unchanged since it was last measured just reuses its cached width, no
  // DOM touched at all; only the row(s) that actually changed get
  // remeasured. The column's own overall width is still recomputed as the
  // true `Math.max` over every row's (mostly cached) width on every render —
  // pure arithmetic over already-known numbers, not DOM work — so a value
  // getting *shorter* correctly narrows the column back down too, not just
  // a one-way "only ever grows" ratchet. A ref, not state: mutating it must
  // never itself trigger a render — the column widths it feeds into are
  // recomputed as an ordinary part of this render anyway.
  const columnWidthCacheRef = useRef<Map<string, Map<string | number, { text: string; width: number }>>>(
    new Map(),
  );
  // State, not a ref (its previous form) — double-click-to-reset (below)
  // needs *removing* an entry to actually trigger a re-render, so the next
  // `withFilter` call recomputes that column's own auto-fit width instead
  // of reusing the stale manual one.
  const [manualColumnWidths, setManualColumnWidths] = useState<Map<string, number>>(() => new Map());
  const fontsReady = useDenseFontsReady();

  // Double-clicking a sortable column's header to resize it also fires two
  // ordinary browser `click` events first (native, unavoidable — a
  // double-click is always "click, click, dblclick"), and each one toggles
  // MUI's own click-to-sort independently, leaving the grid sorted by
  // whatever column was just resized instead of whatever it was actually
  // sorted by before (a real, reported problem — the row order changes
  // out from under a user who only meant to widen a column). Rather than
  // intercepting/debouncing every single click to tell "part of a
  // double-click" from "a genuine click" apart (which would add a felt
  // delay to *all* sorting, everywhere), this instead keeps a rolling
  // 2-entry log of the sort model from just before each of the last two
  // changes, and the double-click handler (`withFilter`, below) restores
  // whichever one is two changes back — i.e. undoes exactly the two
  // clicks' own toggles, leaving any *other*, earlier sort choice alone.
  // A pragmatic Level 1 fix, not a fully general one: it assumes a
  // double-click always produces exactly two sort-model changes, which
  // holds for an ordinary sortable column but would over-correct for one
  // that's already `sortable: false` (rare in this app's own column
  // catalogs) — see `SearchPlan.md`/`Plan.md`'s own note on reconsidering
  // the grid library entirely for Level 2, where this kind of workaround
  // wouldn't be needed in the first place.
  const sortHistoryRef = useRef<GridSortModel[]>([]);
  const previousSortModelRef = useRef<GridSortModel | null>(null);
  useEffect(() => {
    if (!apiRef) return;
    // `useGridApiRef()` is a plain `useRef(null)` (confirmed by reading
    // MUI's own source) — `.current` only becomes non-null once the real
    // `<DataGrid>` this ref is attached to actually mounts, which for a
    // conditionally-rendered grid (Search's own results grid, only shown
    // once a search is submitted) can be well *after* this effect first
    // runs. Calling straight into `apiRef.current.getSortModel()` here —
    // the original version — crashed with `apiRef.current` still `null`,
    // and with no error boundary anywhere in this app, that blanked the
    // entire page. Polling instead of subscribing once also means this
    // survives the grid unmounting and remounting later (e.g. a second,
    // later search in the same still-open window): `lastApi` only
    // resubscribes when the *instance* actually changes, so a still-live
    // subscription isn't torn down and rebuilt every tick for nothing.
    let unsubscribe: (() => void) | undefined;
    let lastApi: typeof apiRef.current = null;
    let cancelled = false;

    function tick() {
      if (cancelled) return;
      const api = apiRef.current;
      if (api && api !== lastApi) {
        unsubscribe?.();
        lastApi = api;
        previousSortModelRef.current = api.getSortModel();
        unsubscribe = api.subscribeEvent("sortModelChange", (newModel) => {
          const history = sortHistoryRef.current;
          history.push(previousSortModelRef.current ?? []);
          if (history.length > 2) history.shift();
          previousSortModelRef.current = newModel;
        });
      } else if (!api) {
        lastApi = null;
      }
    }

    tick();
    const interval = setInterval(tick, 200);
    return () => {
      cancelled = true;
      clearInterval(interval);
      unsubscribe?.();
    };
  }, [apiRef]);
  // Generous while fonts aren't confirmed loaded yet (safe — a column
  // reads as "not quite as tight as it could be" for at most one render,
  // never truncated), removed the moment they are.
  const fontSafetyMargin = fontsReady ? 0 : 20;

  const [filterState, setFilterState] = useState<Record<string, ColumnFilterState>>(
    () => initialFilterState ?? {},
  );
  // `showFilters` is only the *initial* value — V1.2's own real GridControl
  // (Requirements/UserInterfaceWindows.md §3.20) lets the user toggle the
  // filter row's visibility themselves at runtime, via the same right-click
  // menu this reproduces (D1.4-56), independent of whatever an embedding
  // window configured it to start as.
  const [filterVisible, setFilterVisible] = useState(showFilters);

  function setContains(field: string, contains: string) {
    setFilterState((prev) => ({
      ...prev,
      [field]: { contains, exact: (prev[field] ?? EMPTY_COLUMN_FILTER).exact },
    }));
  }
  function setExact(field: string, exact: Set<string> | null) {
    setFilterState((prev) => ({
      ...prev,
      [field]: { contains: (prev[field] ?? EMPTY_COLUMN_FILTER).contains, exact },
    }));
  }

  // Populated below as each column is built (withFilter), then used by
  // getOptionsForField/passesAllFilters — safe only because both are ever
  // *called* later: from event handlers, or from `getFilteredRows` (below)
  // once the caller invokes it, which must not happen until after every
  // `withFilter` call for this render has already run (see that function's
  // own doc comment on `UseDenseGridColumnsResult` — getting this ordering
  // wrong once already shipped a real regression where every filter
  // silently did nothing at all).
  const filterConfigs: Record<string, FilterConfig<T>> = {};

  // V1.2's own ApplyFiltersToRows applies every filter's current value
  // regardless of whether the filter row itself is visible right now
  // (`m_filterIsVisible` gates only the row's own layout/visibility,
  // Requirements/UserInterfaceWindows.md §3.20) — hiding the boxes never
  // silently drops whatever was already typed into them. Matched here:
  // filtering is driven by `filterState` alone, never by `filterVisible`.
  function passesAllFilters(row: T, excludeField?: string): boolean {
    for (const field of Object.keys(filterConfigs)) {
      if (field === excludeField) continue;
      const config = filterConfigs[field];
      const state = filterState[field] ?? EMPTY_COLUMN_FILTER;
      if (!columnFilterPasses(config.getValues(row), state)) return false;
    }
    return true;
  }

  function getOptionsForField(field: string): string[] {
    const config = filterConfigs[field];
    if (!config) return [];
    const values = new Set<string>();
    for (const row of rows) {
      if (!passesAllFilters(row, field)) continue;
      for (const v of config.getValues(row)) values.add(v);
    }
    return sortFilterOptions([...values], config.sortType);
  }

  // The cached counterpart to `measureColumnContentWidth` above, used for a
  // column's own default (non-manual) auto-width — see `columnWidthCacheRef`'s
  // own comment. Deliberately not used by the double-click-to-fit handler
  // below: that measures a different, filtered row set on a rare, explicit
  // user action, where a full fresh scan is the right (and still cheap
  // enough, since it's not happening every render) choice.
  function getCachedColumnContentWidth(field: string, getValues: (row: T) => string[], isBoolean: boolean): number {
    if (isBoolean) return BOOLEAN_COLUMN_WIDTH;
    let cache = columnWidthCacheRef.current.get(field);
    if (!cache) {
      cache = new Map();
      columnWidthCacheRef.current.set(field, cache);
    }
    const liveIds = new Set<string | number>();
    let max = 0;
    for (const row of rows) {
      const id = getRowId(row);
      liveIds.add(id);
      const text = getValues(row).join(", ");
      let entry = cache.get(id);
      if (!entry || entry.text !== text) {
        entry = { text, width: text ? measureTextWidth(text, CELL_FONT_WEIGHT) : 0 };
        cache.set(id, entry);
      }
      if (entry.width > max) max = entry.width;
    }
    // Drop rows no longer present (deleted, or simply not in this — always
    // unfiltered, see measureColumnContentWidth's own comment — `rows` set
    // any more) so a since-removed row's old width can never keep a column
    // artificially wide, and the cache doesn't grow unboundedly forever.
    for (const id of cache.keys()) {
      if (!liveIds.has(id)) cache.delete(id);
    }
    return Math.ceil(max) + CELL_PADDING;
  }

  function withFilter(
    col: GridColDef<T>,
    getValues: (row: T) => string[],
    sortType: FilterSortType,
    widthCap?: number,
    flex?: number,
  ): GridColDef<T> {
    const headerLabel = col.headerName ?? col.field;
    // Registered regardless of `filterVisible` — a filter set while the
    // row was visible must keep working after the user hides it (see
    // passesAllFilters's own comment).
    filterConfigs[col.field] = { getValues, sortType, headerLabel };
    // Always the same renderHeader, never DataGrid's own default one — the
    // label above the filter box must render identically (same bold
    // weight, same position) whether the filter box itself is visible or
    // not; only FilterableHeader's own `filterRowVisible` prop toggles,
    // never which component renders the header at all.
    //
    // minWidth is computed here, from the label text alone — not left to
    // DataGrid's own DOM measurement, since FilterableHeader's own root Box
    // is styled `width: columnWidth` (it always fills whatever the
    // column's *current* width already is), so a DOM measurement of it
    // can only ever report the column's existing width back to itself.
    const headerMinWidth = Math.ceil(measureTextWidth(headerLabel, HEADER_FONT_WEIGHT)) + HEADER_LABEL_PADDING;
    const renderHeader: GridColDef<T>["renderHeader"] = (params) => (
      <FilterableHeader
        label={headerLabel}
        state={filterState[col.field] ?? EMPTY_COLUMN_FILTER}
        onContainsChange={(text) => setContains(col.field, text)}
        onExactChange={(exact) => setExact(col.field, exact)}
        getOptions={() => getOptionsForField(col.field)}
        columnWidth={params.colDef.computedWidth}
        filterRowVisible={filterVisible}
        // Double-click the label to fit this column to its own widest
        // actual content — a deliberately self-contained alternative to
        // MUI's own native "double-click the resize separator to
        // autosize" gesture (`useGridColumnResize.js`), which needs the
        // double-click to land on a specific few-pixel-wide strip
        // *underneath* this very label and, in practice, proved
        // indistinguishable from two ordinary single clicks. Deliberately
        // *not* `widthCap`-limited, unlike the plain auto-computed default
        // below — a column like Description/Detailed Description/Ref URL
        // (TaskGrid.tsx) is capped there specifically so it doesn't
        // dominate the grid *by default*, but a user who explicitly
        // double-clicks it is asking to see the full content, not to be
        // reset back to the same truncating default. Sets an explicit
        // manual width (the same path a drag-resize records into), rather
        // than just clearing any existing override, so it also works the
        // first time — before the user has ever dragged this column at
        // all. Omitted entirely for a `flex` column (`undefined` here when
        // `flex != null`, see below) — it has no manual width of its own
        // to fit; its whole point is filling whatever space is left.
        onLabelDoubleClick={
          flex == null
            ? () => {
                // Measured against every row that currently passes *every*
                // active filter (this column's own included — narrowing
                // this column's own filter first, then double-clicking,
                // still fits to what's actually left) — not the raw,
                // unfiltered `rows`. Deliberately every filtered row, not
                // just whatever's on the DataGrid's own current page: the
                // 100-row paging (D-Win-13/Level2_Implementation/Scope.md's
                // own item on it) is pure client-side display slicing over
                // this same array, so fitting to the *current page* only
                // would look right there and truncate again the moment the
                // user turns the page — fitting to the whole filtered set
                // means it stays correct on every page without a re-fit.
                const visibleRows = rows.filter((row) => passesAllFilters(row));
                const contentWidth = measureColumnContentWidth(getValues, visibleRows, col.type === "boolean");
                const fitWidth = Math.max(headerMinWidth, contentWidth) + fontSafetyMargin;
                setManualColumnWidths((prev) => new Map(prev).set(col.field, fitWidth));
                // Undo the two sort-toggles the double-click's own two
                // constituent clicks just caused (see the sort-history
                // effect above) — only once both have actually happened
                // (`length === 2`; e.g. not yet true for the very first
                // double-click after the grid mounts, before any sort
                // change has been recorded at all).
                if (apiRef && sortHistoryRef.current.length === 2) {
                  apiRef.current.setSortModel(sortHistoryRef.current[0]);
                }
              }
            : undefined
        }
      />
    );

    const manualWidth = manualColumnWidths.get(col.field);

    // `flex` (a consumer's concern, e.g. Search's own Description column,
    // `SearchPlan.md` §4.4/D1.4-78) absorbs whatever width the grid's other
    // columns don't use — the one column that grows/shrinks as the window
    // resizes, instead of DataGrid's own default of leaving dead space
    // after the last column (its "filler" element, otherwise easy to
    // mistake for a genuine blank extra column, which is exactly what
    // prompted this). Only while the user hasn't manually resized this
    // column — resizing it themselves is a `width` choice (below) that
    // MUI DataGrid always ignores when `flex` is also present, so the two
    // are mutually exclusive: once manually resized, `flex` is dropped for
    // good, matching the manual-width override every other column already
    // gets.
    // Unconditional whenever `flex` is given — deliberately ignoring
    // `manualWidth` here, unlike every other column. A flex column's whole
    // job is to keep absorbing whatever width the fixed columns don't use;
    // letting a drag convert it to a fixed `width` (as any other column's
    // own manual override does) is exactly what reopened the phantom-
    // filler gap this was built to prevent in the first place — MUI's own
    // resize handler (`useGridColumnResize.js`) sets `flex: 0` on whichever
    // column is dragged (or double-click-autosized), and since we rebuild
    // `columns` fresh every render regardless, that internal mutation only
    // sticks if *our own* `withFilter` result agrees with it on the next
    // render. `resizable: false` (below) stops the drag/autosize gesture
    // starting at all for this column, rather than starting it and then
    // visibly snapping back once React re-renders.
    if (flex != null) {
      return {
        ...col,
        flex,
        resizable: false,
        minWidth: Math.max(col.minWidth ?? 0, headerMinWidth),
        hideSortIcons: true,
        renderHeader,
      };
    }

    // The actual width: the user's own manual choice if they've resized
    // this column (verbatim — no cap, no recomputation), otherwise
    // whichever is larger of the header's own width and the widest actual
    // cell content across every row, capped at `widthCap` if given.
    // Computed fresh on every render (this function runs on every render
    // regardless) via the per-row cache above, so a `columns` rebuild —
    // unavoidable, since these closures capture live `filterState`/etc. —
    // always lands on the same right answer instead of DataGrid's own
    // ~100px fallback for a column with no explicit `width`, without
    // re-measuring every row's DOM width to get there.
    let width = manualWidth;
    if (width == null) {
      const contentWidth = getCachedColumnContentWidth(col.field, getValues, col.type === "boolean");
      width = Math.max(headerMinWidth, contentWidth) + fontSafetyMargin;
      if (widthCap != null) width = Math.min(width, widthCap);
    }

    return {
      ...col,
      width,
      minWidth: Math.max(col.minWidth ?? 0, headerMinWidth),
      hideSortIcons: true,
      renderHeader,
    };
  }

  return {
    withFilter,
    // Preserves `rows`' own reference when nothing was actually filtered
    // out — `Array.prototype.filter` always allocates a brand-new array,
    // even when every row passed, so without this check a caller whose
    // `rows` prop hasn't genuinely changed (e.g. React Query's cache is
    // still the same object — a re-render caused by something unrelated,
    // such as a mutation's own pending-state flipping) would still hand
    // `DenseDataGrid` a *new* `rows` array identity on every render. MUI
    // DataGrid treats a changed `rows` reference as "the data changed" and
    // re-syncs its whole internal row model from it — including, critically,
    // overwriting whatever `processRowUpdate`'s own resolved value had just
    // written into a row via `updateRows`, if that stale-but-different-
    // identity prop lands in between. That's what turned "commit, done" into
    // "commit, flash back to the old value, then correct itself once the
    // real refetch lands" (ManagePeoplePlan.md's own perf finding, round 2).
    getFilteredRows: () => {
      const filtered = rows.filter((row) => passesAllFilters(row));
      return filtered.length === rows.length ? rows : filtered;
    },
    fitColumnsToContent: (fields: string[]) => {
      const visibleRows = rows.filter((row) => passesAllFilters(row));
      const fitWidths = new Map<string, number>();
      for (const field of fields) {
        const config = filterConfigs[field];
        if (!config) continue;
        const headerMinWidth = Math.ceil(measureTextWidth(config.headerLabel, HEADER_FONT_WEIGHT)) + HEADER_LABEL_PADDING;
        const contentWidth = measureColumnContentWidth(config.getValues, visibleRows, false);
        fitWidths.set(field, Math.max(headerMinWidth, contentWidth) + fontSafetyMargin);
      }
      // Recorded as a manual override too (not just applied via `apiRef`
      // below) so a *later* render's own `withFilter` call — this column's
      // own default-width computation runs on every render regardless —
      // keeps using this fit width rather than recomputing (and reopening)
      // the too-wide default the moment anything else causes a re-render.
      setManualColumnWidths((prev) => {
        const next = new Map(prev);
        for (const [field, width] of fitWidths) next.set(field, width);
        return next;
      });
      // `apiRef.current.setColumnWidth` *also* called directly, not left to
      // the `columns` prop change above alone — confirmed via logging
      // (D1.4-117) that the `columns` prop passed to `<DataGrid>` really did
      // carry the new, narrower `width` on the very next render, yet the
      // column stayed visually at its old (wider) size regardless: this
      // early in a grid's own life (right after first mount, before its own
      // internal column-sizing effects have settled), MUI DataGrid evidently
      // doesn't reliably re-derive `computedWidth` from a `columns` prop
      // change alone the way it does once the grid's been live for a while
      // (a user's own later double-click-to-fit, going through the exact
      // same `columns`-prop mechanism, works reliably — this isn't a defect
      // in that path, just a race specific to how soon after mount this
      // runs). `setColumnWidth` is the grid's own direct, imperative API for
      // exactly this, sidestepping whichever internal effect wasn't done
      // settling yet.
      for (const [field, width] of fitWidths) apiRef?.current?.setColumnWidth(field, width);
    },
    filterVisible,
    setFilterVisible,
    resetFilters: () => setFilterState({}),
    onColumnResize: (field: string, width: number) =>
      setManualColumnWidths((prev) => new Map(prev).set(field, width)),
  };
}

export interface DenseDataGridProps<T> {
  rows: T[];
  columns: GridColDef<T>[];
  getRowId: (row: T) => string | number;
  onRowDoubleClick?: (row: T) => void;
  getRowClassName?: (row: T) => string;
  // Merged after the base chrome sx (border/header fill/font size) — e.g.
  // TaskGrid's own urgency-tint palette.
  sx?: SxProps<Theme>;
  defaultSort?: { field: string; sort: "asc" | "desc" }[];
  // A caller-created ref (`useGridApiRef()`) it also needs itself — e.g.
  // TaskGrid triggers `startCellEditMode` from its own `onCellClick`. When
  // omitted, an internal one is created and used only for this component's
  // own "Copy All".
  apiRef?: ReturnType<typeof useGridApiRef>;
  onColumnResize?: (field: string, width: number) => void;
  // Present only for a grid that has column filtering (TaskGrid) — omit
  // entirely for one that doesn't (Search, `SearchPlan.md` §4.4): the
  // context menu then shows only "Copy All", never
  // "Reset All Filters"/"Show Filter"/"Hide filter" with nothing for them
  // to act on.
  filtering?: {
    filterVisible: boolean;
    onToggleFilterVisible: () => void;
    onResetFilters: () => void;
  };
  // Pass-throughs for an editable grid (TaskGrid only) — absent/inert for a
  // read-only one (Search).
  isCellEditable?: (params: GridCellParams<T>) => boolean;
  onCellClick?: (params: GridCellParams<T>) => void;
  processRowUpdate?: (newRow: T, oldRow: T) => Promise<T>;
  onProcessRowUpdateError?: (err: unknown) => void;
  // D1.4-101 — a one-time seed for which columns start hidden (a plain
  // field list, checked once via a lazy useState initializer, the same
  // "initial value, not a controlled prop" shape `useDenseGridColumns`'s
  // own `initialFilterState` already uses) — a column started this way is
  // still fully togglable afterward via the right-click "Show Column" menu,
  // it just doesn't render on first mount. Columns are always passed to
  // `columns` in full regardless of hidden state; visibility is a pure
  // rendering concern layered on top via MUI's own `columnVisibilityModel`.
  initiallyHiddenFields?: string[];
  // D1.4-118 — `false` (the default) keeps `autoHeight` (D1.4-59): the grid
  // sizes itself to its own content, no internal scrollbar of its own — the
  // right shape for a small embedded grid (Project/Component's own nested
  // TaskGrid) with no independent viewport of its own to fill. `true` is the
  // opposite shape, for a grid that *is* the main content of its own
  // window/panel (All Tasks): it fills 100% of its own parent's height
  // instead, and scrolls its *own* rows internally — critically, this keeps
  // its horizontal scrollbar and footer/pagination pinned to the bottom of
  // that fixed viewport, always visible, rather than wherever the bottom of
  // however-tall an `autoHeight` grid's full row count happens to land (with
  // 100+ rows, well below the fold — reported directly: reaching the
  // horizontal scrollbar needed scrolling *past* every row first). The
  // caller must give this grid's own parent a real, bounded height (e.g.
  // `flex: 1, minHeight: 0` in a flex column) for `height: "100%"` to
  // resolve against — the same containment requirement `AppShell.tsx`
  // itself needed fixing for exactly this reason (`D1.4-111`).
  fillHeight?: boolean;
}

// The shared dense-grid chrome itself (D1.4-73): sizing, border, header
// fill, right-click "Copy All"/filter-menu items, generic over row type T.
export function DenseDataGrid<T>({
  rows,
  columns,
  getRowId,
  onRowDoubleClick,
  getRowClassName,
  sx,
  defaultSort,
  apiRef: externalApiRef,
  onColumnResize,
  filtering,
  isCellEditable,
  onCellClick,
  processRowUpdate,
  onProcessRowUpdateError,
  initiallyHiddenFields,
  fillHeight = false,
}: DenseDataGridProps<T>) {
  const internalApiRef = useGridApiRef();
  const apiRef = externalApiRef ?? internalApiRef;
  const [contextMenu, setContextMenu] = useState<{ mouseX: number; mouseY: number; field: string | null } | null>(
    null,
  );
  // D1.4-101 — hover-opens a flyout of hidden columns from the "Show
  // Column" item, the standard (if slightly manual — MUI has no built-in
  // nested-menu primitive) pattern for a submenu built from plain Menu/
  // MenuItem: a second, independently-anchored Menu, controlled by hover.
  const [showColumnAnchor, setShowColumnAnchor] = useState<HTMLElement | null>(null);
  const [columnVisibilityModel, setColumnVisibilityModel] = useState<GridColumnVisibilityModel>(() => {
    const model: GridColumnVisibilityModel = {};
    for (const field of initiallyHiddenFields ?? []) model[field] = false;
    return model;
  });
  const [copyError, setCopyError] = useState<string | null>(null);

  // Right-click-anywhere-on-the-grid menu (D1.4-56), reproducing V1.2's own
  // GridControl exactly (Requirements/UserInterfaceWindows.md §3.20,
  // GridControl.cs: `dataGridView.ContextMenuStrip = contextMenuStrip1`,
  // attached to the whole grid, not just its filter row) — the standard
  // MUI "anchor a Menu at the click position" recipe.
  function handleContextMenu(event: React.MouseEvent) {
    event.preventDefault();
    if (contextMenu !== null) {
      setContextMenu(null);
      return;
    }
    // D1.4-101 — both a data cell (GridCell.js) and a column header
    // (GridColumnHeaderItem.js) carry their own `data-field` attribute
    // (confirmed by reading MUI's own source); climbing to the nearest one
    // finds which column, if any, the right-click actually landed on,
    // regardless of what specific element inside it — text, an icon — was
    // the literal click target.
    const field = (event.target as HTMLElement).closest("[data-field]")?.getAttribute("data-field") ?? null;
    setContextMenu({ mouseX: event.clientX + 2, mouseY: event.clientY - 6, field });
  }
  function handleCloseContextMenu() {
    setContextMenu(null);
    setShowColumnAnchor(null);
  }

  // V1.2's own "Copy All" (GridControl.cs's selectAllToolStripMenuItem_Click):
  // a tab-separated, newline-separated copy of every currently visible row
  // and column — header row first — straight to the OS clipboard, in
  // whatever the user's current sort order is (V1.2 excludes its own
  // hidden "column zero," the underlying-object column; the equivalent
  // here is any `type: "actions"` column, excluded the same way).
  // `getCellParams(...).formattedValue` mirrors V1.2's own per-column
  // `m_columnFormats`-driven text — what's actually shown in the cell, not
  // the raw underlying value.
  async function handleCopyAll() {
    const dataColumns = columns.filter((c) => c.type !== "actions");
    const header = dataColumns.map((c) => c.headerName ?? c.field).join("\t");
    const lines = apiRef.current.getSortedRowIds().map((id) =>
      dataColumns
        .map((c) => {
          const params = apiRef.current.getCellParams(id, c.field);
          const value = params.formattedValue ?? params.value;
          return value == null ? "" : String(value);
        })
        .join("\t"),
    );
    try {
      await navigator.clipboard.writeText([header, ...lines].join("\n"));
    } catch (err) {
      setCopyError(formatApiError(err, "could not copy to the clipboard."));
    }
    handleCloseContextMenu();
  }

  // D1.4-101 — the column the context menu's own right-click landed on (if
  // any), and whether it's one "Hide Column" should offer at all: the
  // leading Actions column (Delete/Edit icons) is deliberately excluded —
  // it's the only way to act on a row at all, with no other UI path back
  // to that functionality once hidden.
  const contextMenuColumn = contextMenu?.field ? columns.find((c) => c.field === contextMenu.field) : undefined;
  const canHideContextMenuColumn = !!contextMenuColumn && contextMenuColumn.type !== "actions";
  const hiddenColumns = columns.filter((c) => columnVisibilityModel[c.field] === false);

  function hideColumn(field: string) {
    setColumnVisibilityModel((prev) => ({ ...prev, [field]: false }));
    handleCloseContextMenu();
  }
  function showColumn(field: string) {
    setColumnVisibilityModel((prev) => {
      // Deletes rather than sets `true` — MUI treats a field simply absent
      // from the model as visible, and this keeps the model's own size
      // down to just however many columns are actually hidden right now,
      // rather than accumulating a `true` entry for every column ever
      // shown again over a long session.
      const next = { ...prev };
      delete next[field];
      return next;
    });
    handleCloseContextMenu();
  }

  return (
    <Box onContextMenu={handleContextMenu} sx={fillHeight ? { height: "100%" } : undefined}>
      <DataGrid<T>
        apiRef={apiRef}
        rows={rows}
        columnVisibilityModel={columnVisibilityModel}
        onColumnVisibilityModelChange={setColumnVisibilityModel}
        // `fillHeight` (D1.4-118) is the one exception — see its own prop
        // doc comment. Otherwise always sizes to its own content
        // (ProjectsGUIComponent.md §4.5, D1.4-59): a small embedded grid
        // doesn't need a fixed-height box, and a fixed box would either clip
        // a taller grid or leave dead space under a shorter one.
        autoHeight={!fillHeight}
        // No pagination footer once every row already fits on the current
        // page — nothing to page through. Reappears automatically once
        // there's genuinely more than one page's worth of rows.
        hideFooter={rows.length <= 100}
        onColumnResize={
          onColumnResize ? (params) => onColumnResize(params.colDef.field, params.width) : undefined
        }
        // MUI's own double-click-a-separator-to-autosize feature
        // (`useGridColumnResize.js`'s `handleColumnSeparatorDoubleClick`,
        // enabled by default — `disableAutosize` is never set here) fires
        // `columnWidthChange`, not `columnResize` — the latter is only for
        // a *live drag*. Without also recording this one, an autosized
        // column's new width would just be recomputed away again (back to
        // its own default) on this component's very next render, making
        // the double-click appear to do nothing.
        onColumnWidthChange={
          onColumnResize ? (params) => onColumnResize(params.colDef.field, params.width) : undefined
        }
        getRowId={getRowId}
        columns={columns}
        onCellClick={onCellClick}
        onCellDoubleClick={
          onRowDoubleClick
            ? (params) => {
                if (params.colDef.type === "actions") return;
                onRowDoubleClick(params.row);
              }
            : undefined
        }
        rowHeight={DENSE_ROW_HEIGHT}
        columnHeaderHeight={
          filtering ? (filtering.filterVisible ? HEADER_HEIGHT : HEADER_HEIGHT_NO_FILTER_ROW) : HEADER_HEIGHT_NO_FILTER_ROW
        }
        disableColumnMenu
        disableColumnFilter
        // Row *selection* (a separate concept from cell focus) is what
        // paints a flat grey over a clicked row's own background colour
        // (e.g. TaskGrid's urgency tint) — disabling this leaves the
        // focused cell's own border (a separate, cell-focus concern)
        // exactly as it was.
        disableRowSelectionOnClick
        showColumnVerticalBorder
        showCellVerticalBorder
        processRowUpdate={processRowUpdate}
        onProcessRowUpdateError={onProcessRowUpdateError}
        isCellEditable={isCellEditable}
        initialState={{
          pagination: { paginationModel: { pageSize: 100 } },
          sorting: { sortModel: defaultSort ?? [] },
        }}
        sortingOrder={["asc", "desc"]}
        getRowClassName={getRowClassName ? (params) => getRowClassName(params.row) : undefined}
        sx={{
          fontSize: DENSE_FONT_SIZE,
          // A fine, dark grey outline around the whole grid — MUI's own
          // default border reads as too faint to clearly separate the grid
          // from whatever it's embedded in.
          border: "1px solid rgba(0,0,0,0.4)",
          "& .MuiDataGrid-columnHeader": { paddingLeft: 0, paddingRight: 0 },
          // The header/sort row's own grey fill lives on FilterableHeader's
          // own label Box instead (GridColumnFilter.tsx) — not here — since
          // this container wraps the filter row too, and that needs to
          // stay white, not shaded the same as the label above it.
          "& .MuiDataGrid-columnHeaders": {
            bgcolor: "#fff",
            // Darkens the vertical divider *between header cells specifically*
            // (both the label row and, once darkened this way, the filter row
            // underneath it too — the two aren't independently stylable, since
            // MUI paints one border per header cell spanning its full height,
            // not two). Overriding MUI's own CSS variable directly, scoped to
            // this container only (never touching `.MuiDataGrid-virtualScroller`,
            // the data rows' own separate container, so the main grid's own
            // vertical lines stay at MUI's default shade) — not a custom
            // element layered on top of MUI's own separator, which is what
            // three earlier attempts tried and each broke under a filter-row-
            // visible/hidden difference that resisted diagnosis from source
            // alone (confirmed via DevTools: MUI's own `.MuiDataGrid-
            // columnSeparator` sits above anything `colDef.renderHeader`
            // produces, in a stacking context our own content has no way to
            // out-rank — the border MUI paints on the header cell itself,
            // not that separator overlay, is what was actually ever visible,
            // and is what this recolours directly instead of fighting).
            "--DataGrid-rowBorderColor": "rgba(0,0,0,0.4)",
          },
          ...sx,
        }}
      />
      <Snackbar open={!!copyError} autoHideDuration={6000} onClose={() => setCopyError(null)}>
        <Alert severity="error" onClose={() => setCopyError(null)}>
          {copyError}
        </Alert>
      </Snackbar>
      <Menu
        open={contextMenu !== null}
        onClose={handleCloseContextMenu}
        anchorReference="anchorPosition"
        anchorPosition={contextMenu !== null ? { top: contextMenu.mouseY, left: contextMenu.mouseX } : undefined}
        // Zero-duration close: with the default fade, the outer menu stays
        // mounted and hoverable for the whole exit transition while its own
        // conditionally-rendered items (Hide Column, Show Column) are
        // simultaneously re-evaluated against the state that just changed
        // (contextMenu/hiddenColumns) — so an item can vanish or shift under
        // a cursor that hasn't moved, and the browser fires a real mouseenter
        // on whatever now sits there. That's what reopened "Show Column"'s
        // flyout right after Hide Column ran. Closing instantly removes the
        // window where that stray hover can happen.
        transitionDuration={0}
      >
        {filtering && (
          <MenuItem
            onClick={() => {
              filtering.onResetFilters();
              handleCloseContextMenu();
            }}
            onMouseEnter={() => setShowColumnAnchor(null)}
            dense
          >
            Reset All Filters
          </MenuItem>
        )}
        <MenuItem onClick={handleCopyAll} onMouseEnter={() => setShowColumnAnchor(null)} dense>
          Copy All
        </MenuItem>
        {filtering && (
          <MenuItem
            onClick={() => {
              filtering.onToggleFilterVisible();
              handleCloseContextMenu();
            }}
            onMouseEnter={() => setShowColumnAnchor(null)}
            dense
          >
            <ListItemText>{filtering.filterVisible ? "Hide filter" : "Show Filter"}</ListItemText>
          </MenuItem>
        )}
        {canHideContextMenuColumn && (
          <MenuItem
            onClick={() => hideColumn(contextMenu!.field!)}
            onMouseEnter={() => setShowColumnAnchor(null)}
            dense
          >
            Hide Column
          </MenuItem>
        )}
        {hiddenColumns.length > 0 && (
          <MenuItem
            dense
            onMouseEnter={(event) => setShowColumnAnchor(event.currentTarget)}
            sx={{ justifyContent: "space-between" }}
          >
            Show Column
            <Box component="span" sx={{ ml: 2, fontSize: 12, opacity: 0.6 }}>
              ▶
            </Box>
          </MenuItem>
        )}
      </Menu>
      {/* D1.4-101 — "Show Column"'s own flyout, a second, independently-
          anchored Menu rather than a true nested submenu (MUI has no
          built-in primitive for one): opened by hovering "Show Column"
          above, closed by hovering elsewhere in the outer menu (each of
          its other items also clears `showColumnAnchor` on its own
          onMouseEnter, below) or by picking one of its own items. */}
      <Menu
        open={showColumnAnchor !== null}
        anchorEl={showColumnAnchor}
        onClose={() => setShowColumnAnchor(null)}
        anchorOrigin={{ vertical: "top", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "left" }}
        // Never auto-focuses on open — that would steal focus away from
        // the outer menu's own item the moment a hover (not a click)
        // opens this one, which reads as broken keyboard/visual focus.
        autoFocus={false}
        disableAutoFocusItem
        // See the outer menu's own transitionDuration comment above — the
        // same stray-hover-during-close hazard applies to this flyout too
        // (e.g. un-hiding the last hidden column leaves it briefly visible
        // and hoverable while its own content has already gone empty).
        transitionDuration={0}
      >
        {hiddenColumns.map((c) => (
          <MenuItem key={c.field} dense onClick={() => showColumn(c.field)}>
            {c.headerName ?? c.field}
          </MenuItem>
        ))}
      </Menu>
    </Box>
  );
}
