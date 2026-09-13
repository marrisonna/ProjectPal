import { useEffect, useLayoutEffect, useMemo, useRef, useState, type ChangeEvent, type KeyboardEvent } from "react";
import { useParams } from "react-router";
import Box from "@mui/material/Box";
import CircularProgress from "@mui/material/CircularProgress";
import Typography from "@mui/material/Typography";
import { DenseButton } from "../../components/DenseField";
import { DENSE_FONT_SIZE } from "../../theme/theme";
import { useAuth } from "../../auth/AuthContext";
import {
  useAllDependencies,
  useAllTaskResources,
  useProjects,
  useTasks,
} from "../../api/hooks";
import { addCalendarDays, buildScheduleGraph, formatDdMmmYy } from "../../lib/schedule";
import { BAR_HEIGHT, PIXELS_PER_DAY, ROW_HEIGHT, buildGanttLayout, computeGridLines, type GanttBar } from "../../lib/ganttLayout";
import { openItemWindow, useSingletonWindowIdentity } from "../../lib/windowNav";

const DEFAULT_LABEL_COLUMN_WIDTH = 220;
const MIN_LABEL_COLUMN_WIDTH = 0;
const MAX_LABEL_COLUMN_WIDTH = 600;
// Dragged narrower than this and released ("completely contracted") turns
// "Show Names" off entirely, rather than leaving an unusably thin column.
const LABEL_COLUMN_COLLAPSE_THRESHOLD = 24;
const RESIZE_HANDLE_WIDTH = 6;
const CHART_RIGHT_PADDING = 40;
const BASE_FONT_SIZE = 11;
const MIN_ZOOM = 10;
const MAX_ZOOM = 1000;
const TODAY_BUTTON_FRACTION = 0.25;
// The Project "extent" guide lines' own colour — same strokeWidth-1
// black as the week/month grid below, just more opaque.
const EXTENT_LINE_COLOUR = "rgba(0,0,0,0.3)";
// Same colour/thickness family as EXTENT_LINE_COLOUR, considerably more
// transparent, since these cover the whole chart rather than one bar's
// own edges. Month lines reuse this same colour, just twice as thick.
const GRID_LINE_COLOUR = "rgba(0,0,0,0.07)";
const WEEK_LINE_WIDTH = 1;
const MONTH_LINE_WIDTH = 2;
// "Boxed" mode (V1.2's own Project rendering, Libs/PlanDisplay/Project.cs
// — a Project is a container its child Tasks/Projects are visually drawn
// inside, not just a bar at its own row). The normal Project bar colour
// (ganttLayout.ts's PROJECT_BAR_COLOUR, "#607d8b") is a low-saturation
// blue-grey; washed out at low opacity it reads as plain grey rather than
// as a colour. This uses a more saturated steel/denim blue instead — still
// muted, not a bright/saturated colour — at 15% opacity, so nested
// Projects — each painted over its parent's own box — read as
// progressively darker through plain alpha compositing, with no
// per-depth colour needed.
const PROJECT_BOX_FILL = "rgba(52, 108, 158, 0.15)";

function clampZoom(value: number): number {
  return Math.min(MAX_ZOOM, Math.max(MIN_ZOOM, value));
}

function clampLabelColumnWidth(value: number): number {
  return Math.min(MAX_LABEL_COLUMN_WIDTH, Math.max(MIN_LABEL_COLUMN_WIDTH, value));
}

// D1.4-24: the standalone Plan Display's own singleton window (matching
// Task Detail's D1.4-8 pattern), not in-place navigation — opened via
// AppShell's "Plan" button (openListWindow("plan")) or, once Project
// Detail exists (Stage 4), a per-Project "Gantt Display" action
// (openItemWindow("plan", projectId)).
export function PlanPage() {
  const { projectId: projectIdParam } = useParams<{ projectId?: string }>();
  const projectId = projectIdParam ? Number(projectIdParam) : null;
  useSingletonWindowIdentity(projectId != null ? `plan-${projectId}` : "plan-list");

  const { person } = useAuth();
  const { data: allProjects, isLoading: projectsLoading } = useProjects();
  const { data: tasks, isLoading: tasksLoading } = useTasks();
  const { data: dependencies, isLoading: dependenciesLoading } = useAllDependencies();
  const { data: allTaskResources } = useAllTaskResources();

  // Only show Projects on a Team the caller actually belongs to (any role)
  // — matches every other screen's own read scope, rather than exposing
  // every Team's Projects to everyone.
  const memberTeamIds = useMemo(
    () => new Set((person?.team_roles ?? []).map((tr) => tr.team_id)),
    [person],
  );
  const projects = useMemo(
    () => allProjects?.filter((p) => memberTeamIds.has(p.team_id)),
    [allProjects, memberTeamIds],
  );

  const resourceCountByTaskId = useMemo(() => {
    const map = new Map<number, number>();
    for (const r of allTaskResources ?? []) {
      map.set(r.task_id, (map.get(r.task_id) ?? 0) + 1);
    }
    return map;
  }, [allTaskResources]);

  const layout = useMemo(() => {
    if (!projects || !tasks || !dependencies) return null;
    const graph = buildScheduleGraph(tasks, projects, dependencies, resourceCountByTaskId);
    return buildGanttLayout(graph, dependencies, projectId);
  }, [projects, tasks, dependencies, resourceCountByTaskId, projectId]);

  const labelAreaRef = useRef<HTMLDivElement>(null);
  const drawingAreaRef = useRef<HTMLDivElement>(null);
  const ganttAreaRef = useRef<HTMLDivElement>(null);

  // Zoom (D1.4-24 follow-up): the layout this page renders from
  // (lib/ganttLayout.ts) is always computed at a fixed 100%-baseline —
  // zoom is purely a render-time linear scale applied here, per axis,
  // rather than something ganttLayout.ts itself needs to know about.
  const [zoomX, setZoomX] = useState(100);
  const [zoomY, setZoomY] = useState(100);
  // Live copies for the wheel handler below to read — that handler is
  // attached once (see its own effect's comment on why: it can't depend
  // on `[zoomX, zoomY]` without a real bug), so it can't rely on closing
  // over a particular render's `zoomX`/`zoomY`; refs are always current
  // regardless of which render's closure ends up holding the function.
  const zoomXRef = useRef(zoomX);
  const zoomYRef = useRef(zoomY);
  zoomXRef.current = zoomX;
  zoomYRef.current = zoomY;

  // "Boxed" mode (see PROJECT_BOX_FILL above) — off by default, today's
  // thin-bar-per-row rendering.
  const [boxed, setBoxed] = useState(false);

  // "Show Names": the label column's own width is user-resizable by
  // dragging the handle between it and the drawing area (mousedown/move/up
  // below); dragging it all the way down to (near) zero and releasing
  // turns "Show Names" off entirely, same as unchecking it, rather than
  // leaving an unusable sliver. Re-checking it always comes back at the
  // one fixed default width — there's no "remembered" width to restore,
  // by design (the user asked for exactly this behaviour).
  const [showNames, setShowNames] = useState(true);
  const [labelColumnWidth, setLabelColumnWidth] = useState(DEFAULT_LABEL_COLUMN_WIDTH);
  const effectiveLabelColumnWidth = showNames ? labelColumnWidth : 0;
  const labelResizeRef = useRef<{ startClientX: number; startWidth: number } | null>(null);

  function handleLabelResizeMouseDown(event: { clientX: number }) {
    labelResizeRef.current = { startClientX: event.clientX, startWidth: labelColumnWidth };
    function handleMouseMove(moveEvent: MouseEvent) {
      const drag = labelResizeRef.current;
      if (!drag) return;
      const newWidth = clampLabelColumnWidth(drag.startWidth + (moveEvent.clientX - drag.startClientX));
      setLabelColumnWidth(newWidth);
    }
    function handleMouseUp() {
      labelResizeRef.current = null;
      setLabelColumnWidth((currentWidth) => {
        if (currentWidth <= LABEL_COLUMN_COLLAPSE_THRESHOLD) {
          setShowNames(false);
          return DEFAULT_LABEL_COLUMN_WIDTH;
        }
        return currentWidth;
      });
      window.removeEventListener("mousemove", handleMouseMove);
      window.removeEventListener("mouseup", handleMouseUp);
    }
    window.addEventListener("mousemove", handleMouseMove);
    window.addEventListener("mouseup", handleMouseUp);
  }

  // The name shown in the read-only label below the zoom controls while
  // hovering a Task/Project bar — cleared the moment the mouse leaves it.
  const [hoveredLabel, setHoveredLabel] = useState("");
  // The date under the cursor, tracked continuously across the whole
  // drawing area (not just while over a bar) — a plain `onMouseMove`
  // suffices here (unlike the wheel handler above, nothing needs
  // preventDefault, so there's no passive-listener obstacle to a normal
  // JSX handler).
  const [hoveredDate, setHoveredDate] = useState("");

  function handleDrawingAreaMouseMove(event: { clientX: number }) {
    const drawingArea = drawingAreaRef.current;
    if (!drawingArea || !layout?.minDate) return;
    const rect = drawingArea.getBoundingClientRect();
    const contentX = Math.max(0, drawingArea.scrollLeft + (event.clientX - rect.left));
    const effectivePixelsPerDay = PIXELS_PER_DAY * (zoomXRef.current / 100);
    const dayOffset = Math.floor(contentX / effectivePixelsPerDay);
    setHoveredDate(formatDdMmmYy(addCalendarDays(layout.minDate, dayOffset)));
  }

  // Applying a new zoom must keep one particular point still under the
  // same screen position afterwards — the *anchor*. For a wheel-driven
  // zoom that's wherever the mouse cursor was at the time (so the user
  // can zoom into whatever they're pointing at); for every other way
  // zoom can change (typing a percentage, Zoom Reset), there's no cursor
  // position to anchor to, so it falls back to the viewport's own centre
  // (`anchorClientX`/`anchorClientY` omitted). The anchor is stored as a
  // *day offset* (X) / *row offset* (Y) from the layout's own minDate/
  // row 0 (content-relative, stays meaningful across a scale change)
  // together with its own pixel offset *within the visible pane*
  // (viewport-relative, i.e. "40px down from the top of the pane") — the
  // pair is what's needed to put that same content back at that same
  // on-screen position after the scale changes. Read before the state
  // update; applied in a useLayoutEffect once the new scale has actually
  // rendered (changing the SVGs' width/height), via these refs rather
  // than effect deps, so the scroll-restore doesn't need `layout: null`
  // guards duplicated.
  interface PendingZoomAnchor {
    contentOffset: number;
    viewportOffset: number;
  }
  const pendingHorizontalAnchorRef = useRef<PendingZoomAnchor | null>(null);
  const pendingVerticalAnchorRef = useRef<PendingZoomAnchor | null>(null);

  function applyZoomX(newZoomX: number, anchorClientX?: number) {
    const drawingArea = drawingAreaRef.current;
    if (drawingArea) {
      const rect = drawingArea.getBoundingClientRect();
      const viewportOffset = (anchorClientX ?? rect.left + rect.width / 2) - rect.left;
      const effectivePixelsPerDay = PIXELS_PER_DAY * (zoomXRef.current / 100);
      pendingHorizontalAnchorRef.current = {
        contentOffset: (drawingArea.scrollLeft + viewportOffset) / effectivePixelsPerDay,
        viewportOffset,
      };
    }
    setZoomX(clampZoom(newZoomX));
  }

  function applyZoomY(newZoomY: number, anchorClientY?: number) {
    const pane = drawingAreaRef.current ?? labelAreaRef.current;
    if (pane) {
      const rect = pane.getBoundingClientRect();
      const viewportOffset = (anchorClientY ?? rect.top + rect.height / 2) - rect.top;
      const effectiveRowHeight = ROW_HEIGHT * (zoomYRef.current / 100);
      pendingVerticalAnchorRef.current = {
        contentOffset: (pane.scrollTop + viewportOffset) / effectiveRowHeight,
        viewportOffset,
      };
    }
    setZoomY(clampZoom(newZoomY));
  }

  useLayoutEffect(() => {
    const drawingArea = drawingAreaRef.current;
    const anchor = pendingHorizontalAnchorRef.current;
    if (!drawingArea || !anchor) return;
    const effectivePixelsPerDay = PIXELS_PER_DAY * (zoomX / 100);
    drawingArea.scrollLeft = Math.max(0, anchor.contentOffset * effectivePixelsPerDay - anchor.viewportOffset);
    pendingHorizontalAnchorRef.current = null;
  }, [zoomX]);

  useLayoutEffect(() => {
    const anchor = pendingVerticalAnchorRef.current;
    if (!anchor) return;
    const effectiveRowHeight = ROW_HEIGHT * (zoomY / 100);
    for (const pane of [drawingAreaRef.current, labelAreaRef.current]) {
      if (!pane) continue;
      pane.scrollTop = Math.max(0, anchor.contentOffset * effectiveRowHeight - anchor.viewportOffset);
    }
    pendingVerticalAnchorRef.current = null;
  }, [zoomY]);

  // Ctrl+wheel zooms horizontally, Shift+wheel zooms vertically, both
  // held zooms both — attached as a real (non-passive) native listener,
  // not JSX onWheel: React attaches its own wheel listeners as passive,
  // so `preventDefault` inside a synthetic handler is silently ignored,
  // and preventDefault here is exactly what stops the browser's own
  // native page-zoom (Ctrl+wheel) / horizontal-scroll (Shift+wheel).
  // Attached to the Gantt area's own two-pane container specifically
  // (not window/document) — the browser only ever dispatches a wheel
  // event here when the cursor is actually over this element or a
  // descendant, so elsewhere on the page (the title, the controls row
  // below), a Ctrl/Shift+wheel keeps doing whatever the browser normally
  // does there, with nothing extra needed to make that true.
  //
  // Deliberately depends on `[layout]`, not `[zoomX, zoomY]`: the Gantt
  // area only exists in the DOM once `layout` is non-null (it's behind
  // the loading guard below), so `ganttAreaRef.current` first becomes
  // non-null on the very render where `layout` changes from null to a
  // real value — `zoomX`/`zoomY` are still 100/100 at that exact moment
  // and don't themselves change on that render, so an effect keyed on
  // them would never re-run to pick up the now-attached ref, and the
  // listener would silently never attach at all. Reading `zoomXRef`/
  // `zoomYRef` inside the handler (rather than `zoomX`/`zoomY` directly)
  // is what makes it safe for this effect to attach the listener once
  // and still always act on the *current* zoom, not whatever it was
  // when the effect last ran.
  useEffect(() => {
    const ganttArea = ganttAreaRef.current;
    if (!ganttArea) return;
    function handleWheel(event: WheelEvent) {
      if (!event.ctrlKey && !event.shiftKey) return;
      event.preventDefault();
      const factor = 1.0015 ** -event.deltaY;
      // Anchored to the cursor, not the viewport centre — the whole
      // point being to let the user zoom into whatever they're pointing
      // at, per the user's own framing of this feature.
      if (event.ctrlKey) applyZoomX(zoomXRef.current * factor, event.clientX);
      if (event.shiftKey) applyZoomY(zoomYRef.current * factor, event.clientY);
    }
    ganttArea.addEventListener("wheel", handleWheel, { passive: false });
    return () => ganttArea.removeEventListener("wheel", handleWheel);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [layout]);

  function scrollToToday(fraction: number) {
    const drawingArea = drawingAreaRef.current;
    if (!drawingArea || !layout) return;
    const todayXScaled = layout.todayX * (zoomX / 100);
    const maxScrollLeft = Math.max(0, drawingArea.scrollWidth - drawingArea.clientWidth);
    drawingArea.scrollLeft = Math.min(
      maxScrollLeft,
      Math.max(0, todayXScaled - fraction * drawingArea.clientWidth),
    );
  }

  // Real seed data spans years (old, un-shifted legacy dates alongside
  // dates auto-computed far into the future as "today" drifts past the
  // seed data's own anchor point — Phase 5's own documented "further
  // consideration") — and rows are ordered alphabetically, not
  // chronologically, so a bar's row position tells you nothing about
  // where it sits horizontally. Left at the default scroll position
  // (x=0, i.e. the single earliest bar across the whole layout), most of
  // what's initially on screen can easily be bars positioned thousands of
  // pixels further right — indistinguishable from a genuinely blank
  // chart. Scroll so "today" sits 25% in from the left by default
  // instead — the same positioning the "Today" button below applies —
  // since that's the one reference point every row is positioned
  // relative to.
  useEffect(() => {
    if (!layout) return;
    scrollToToday(TODAY_BUTTON_FRACTION);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [layout]);

  const rootProjectName = projectId != null ? projects?.find((p) => p.project_id === projectId)?.name : null;

  if (projectsLoading || tasksLoading || dependenciesLoading || !layout) {
    return <CircularProgress sx={{ m: 2 }} />;
  }

  const chartWidthBase = Math.max(
    400,
    ...layout.bars.map((b) => b.x + b.width),
    layout.todayX,
  ) + CHART_RIGHT_PADDING;
  const chartHeightBase = Math.max(ROW_HEIGHT, layout.rowCount * ROW_HEIGHT);
  const gridLines = computeGridLines(layout.minDate, chartWidthBase);

  const scaleX = zoomX / 100;
  const scaleY = zoomY / 100;
  const chartWidth = chartWidthBase * scaleX;
  const chartHeight = chartHeightBase * scaleY;
  const barHeight = BAR_HEIGHT * scaleY;
  const fontSize = BASE_FONT_SIZE * scaleY;
  const todayX = layout.todayX * scaleX;

  return (
    <Box sx={{ p: 1, height: "100vh", boxSizing: "border-box", display: "flex", flexDirection: "column" }}>
      <Typography variant="subtitle1" sx={{ mb: 1, flexShrink: 0 }}>
        {rootProjectName ?? "Top Level Projects"}
      </Typography>
      <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 1, flexShrink: 0 }}>
        <Typography variant="body2">Zoom:</Typography>
        <Typography variant="body2">H</Typography>
        <ZoomPercentInput value={zoomX} onCommit={applyZoomX} />
        <Typography variant="body2">V</Typography>
        <ZoomPercentInput value={zoomY} onCommit={applyZoomY} />
        <DenseButton
          onClick={() => {
            applyZoomX(100);
            applyZoomY(100);
          }}
        >
          Zoom Reset
        </DenseButton>
        <DenseButton onClick={() => scrollToToday(TODAY_BUTTON_FRACTION)}>Today</DenseButton>
        <Box component="label" sx={{ display: "flex", alignItems: "center", gap: "4px", cursor: "pointer", userSelect: "none" }}>
          <Box
            component="input"
            type="checkbox"
            checked={showNames}
            onChange={(event: ChangeEvent<HTMLInputElement>) => {
              const checked = event.target.checked;
              setShowNames(checked);
              // Re-checking always comes back at the fixed default width,
              // never whatever width it happened to be dragged to before
              // being unchecked — there's nothing to "restore" here.
              if (checked) setLabelColumnWidth(DEFAULT_LABEL_COLUMN_WIDTH);
            }}
            sx={{ m: 0 }}
          />
          <Typography variant="body2">Show Names</Typography>
        </Box>
        <Box component="label" sx={{ display: "flex", alignItems: "center", gap: "4px", cursor: "pointer", userSelect: "none" }}>
          <Box
            component="input"
            type="checkbox"
            checked={boxed}
            onChange={(event: ChangeEvent<HTMLInputElement>) => setBoxed(event.target.checked)}
            sx={{ m: 0 }}
          />
          <Typography variant="body2">Boxed</Typography>
        </Box>
      </Box>
      {/* Date under the cursor (left, right-aligned, right edge 2em
          before the drawing area's own left edge) and the hovered
          Task/Project's name (right, V1.2's own Gantt hover-caption
          syntax — lib/ganttLayout.ts's hoverLabel/buildProjectChain),
          whose own left edge lines up with the drawing area's left edge
          below (not the row-label column to its left) — the date
          control's width plus the 2em gap after it together add up to
          the same total offset (that column's width, box-sizing:
          border-box already folding in its own right border; plus the
          resize handle's own width when the label column is shown at
          all; plus the Gantt area's own 1px outer left border). Tracks
          the label column's own resizable width, not a fixed constant —
          see `effectiveLabelColumnWidth` above. */}
      <Box sx={{ display: "flex", alignItems: "center", mb: 1, flexShrink: 0 }}>
        <Box
          component="input"
          type="text"
          readOnly
          value={hoveredDate}
          sx={{
            width: `calc(${effectiveLabelColumnWidth + (showNames ? RESIZE_HANDLE_WIDTH : 0) + 1}px - 2em)`,
            flexShrink: 0,
            textAlign: "right",
            fontSize: DENSE_FONT_SIZE,
            fontFamily: "inherit",
            border: "none",
            outline: "none",
            bgcolor: "transparent",
            px: 0,
            py: "3px",
            color: "rgba(0,0,0,0.6)",
          }}
        />
        <Box sx={{ width: "2em", flexShrink: 0 }} />
        <Box
          component="input"
          type="text"
          readOnly
          value={hoveredLabel}
          sx={{
            flexGrow: 1,
            fontSize: DENSE_FONT_SIZE,
            fontFamily: "inherit",
            border: "none",
            outline: "none",
            bgcolor: "transparent",
            px: 0,
            py: "3px",
            color: "rgba(0,0,0,0.6)",
          }}
        />
      </Box>
      {layout.bars.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Nothing to show — no active Projects or Tasks with resolvable dates.
        </Typography>
      ) : (
        // Two genuinely separate regions, not an overlapping/sticky one —
        // a `position: sticky` label column was tried first and rejected:
        // pinning it to the drawing area's own scroll viewport means it
        // necessarily paints over whatever bar has scrolled underneath
        // it. A single shared outer vertical scroll (both panes full
        // content height, sized by their own tall SVG) was tried next and
        // also rejected: the drawing area's own horizontal scrollbar then
        // sits at the bottom of its full ~8000px content height, not the
        // bottom of what's actually visible — invisible until scrolled
        // all the way down. Instead: both panes get the *same* height and
        // scroll independently — the label column vertically only, the
        // drawing area both ways — with their vertical scroll positions
        // mirrored via `syncScrollTop` so rows stay aligned. That shared
        // height used to be a fixed `70vh`, which didn't actually track
        // the window: growing/shrinking the window changes the header's
        // own (roughly constant-px) height while `vh` scales with the
        // *whole* window, so the gap between the two grew or shrank with
        // it — at small window heights the panes' fixed 70vh could exceed
        // the space actually left below the header, overflowing the page
        // and pushing the drawing area's own horizontal scrollbar off the
        // bottom of the screen behind a page-level vertical scrollbar.
        // Fixed by making the outer page a flex column pinned to `100vh`
        // (`flexShrink: 0` on the title/controls rows above), and letting
        // this row fill whatever's actually left (`flexGrow: 1`,
        // `minHeight: 0` — required so a flex child can shrink to fit
        // instead of growing to its content's height) — the panes below
        // then simply take `height: "100%"` of that.
        <Box ref={ganttAreaRef} sx={{ display: "flex", flexGrow: 1, minHeight: 0, border: "1px solid rgba(0,0,0,0.12)", borderRadius: "6px" }}>
          {showNames && (
            <>
              <Box
                ref={labelAreaRef}
                onScroll={() => syncScrollTop(labelAreaRef.current!, drawingAreaRef.current)}
                sx={{ flexShrink: 0, width: labelColumnWidth, height: "100%", overflowY: "auto", overflowX: "hidden", borderRight: "1px solid rgba(0,0,0,0.12)" }}
              >
                <Box component="svg" width={labelColumnWidth} height={chartHeight} sx={{ display: "block" }}>
                  {layout.bars.map((bar) => (
                    <text
                      key={`label-${bar.kind}-${bar.id}`}
                      x={8 + bar.depth * 14}
                      y={bar.y * scaleY + barHeight + 1}
                      fontSize={fontSize}
                      fontWeight={bar.kind === "project" ? 700 : 400}
                    >
                      {bar.label}
                    </text>
                  ))}
                </Box>
              </Box>
              <Box
                onMouseDown={handleLabelResizeMouseDown}
                sx={{
                  flexShrink: 0,
                  width: `${RESIZE_HANDLE_WIDTH}px`,
                  cursor: "col-resize",
                  bgcolor: "rgba(0,0,0,0.04)",
                  "&:hover": { bgcolor: "rgba(0,0,0,0.15)" },
                }}
              />
            </>
          )}
          <Box
            ref={drawingAreaRef}
            onScroll={() => syncScrollTop(drawingAreaRef.current!, labelAreaRef.current)}
            onMouseMove={handleDrawingAreaMouseMove}
            onMouseLeave={() => setHoveredDate("")}
            sx={{ flexGrow: 1, height: "100%", overflow: "auto" }}
          >
            {/* V1.2's own Gantt background (Libs/PlanDisplay/Project.cs's
                `OuterBrush`, ARGB(64,200,255,210) painted behind each
                project's bars) composites to roughly #f1fff4 over a white
                page — this is that colour, nudged a touch lighter still,
                per request. On the SVG itself (the full scrollable
                canvas), not the pane around it, so it covers the whole
                chart area and scrolls with the content, not just
                whatever's currently in the viewport. Only when *not*
                Boxed, though — stacking this green wash underneath the
                boxes' own translucent bluish-grey fill is exactly what
                made Boxed mode look muddy on first try: two different
                hues layered as translucent washes, not simply "too much"
                of one colour. Boxed mode's own nested boxes already
                carry the same "how deep is this" visual job the canvas
                tint exists for in the plain view, so there's nothing lost
                by only using one or the other. */}
            <Box component="svg" width={chartWidth} height={chartHeight} sx={{ display: "block", bgcolor: boxed ? "#fff" : "#f5fff7" }}>
              {/* Week (Monday) and month (1st) grid lines — drawn first,
                  so they sit behind everything else (the extent guides,
                  arrows, and bars all paint over them). */}
              {gridLines.weekLineXs.map((x) => (
                <line
                  key={`week-${x}`}
                  x1={x * scaleX}
                  y1={0}
                  x2={x * scaleX}
                  y2={chartHeight}
                  stroke={GRID_LINE_COLOUR}
                  strokeWidth={WEEK_LINE_WIDTH}
                />
              ))}
              {gridLines.monthLineXs.map((x) => (
                <line
                  key={`month-${x}`}
                  x1={x * scaleX}
                  y1={0}
                  x2={x * scaleX}
                  y2={chartHeight}
                  stroke={GRID_LINE_COLOUR}
                  strokeWidth={MONTH_LINE_WIDTH}
                />
              ))}
              {/* Redundant in Boxed mode — a box's own border already
                  marks the same left/right edges these lines would. */}
              {!boxed &&
                layout.bars.map((bar) =>
                  bar.kind === "project" && bar.subtreeBottomY != null ? (
                    <g key={`extent-${bar.id}`}>
                      <line
                        x1={bar.x * scaleX}
                        y1={bar.y * scaleY + barHeight}
                        x2={bar.x * scaleX}
                        y2={bar.subtreeBottomY * scaleY}
                        stroke={EXTENT_LINE_COLOUR}
                        strokeWidth={1}
                      />
                      <line
                        x1={(bar.x + bar.width) * scaleX}
                        y1={bar.y * scaleY + barHeight}
                        x2={(bar.x + bar.width) * scaleX}
                        y2={bar.subtreeBottomY * scaleY}
                        stroke={EXTENT_LINE_COLOUR}
                        strokeWidth={1}
                      />
                    </g>
                  ) : null,
                )}
              <line
                x1={todayX}
                y1={0}
                x2={todayX}
                y2={chartHeight}
                stroke="#d32f2f"
                strokeWidth={1}
                strokeDasharray="4 3"
              />
              {layout.arrows.map((arrow, index) => (
                <line
                  key={`arrow-${index}`}
                  x1={arrow.x1 * scaleX}
                  y1={arrow.y1 * scaleY}
                  x2={arrow.x2 * scaleX}
                  y2={arrow.y2 * scaleY}
                  stroke="rgba(0,0,0,0.4)"
                  strokeWidth={1}
                  markerEnd="url(#gantt-arrow-head)"
                />
              ))}
              <defs>
                <marker id="gantt-arrow-head" markerWidth={6} markerHeight={6} refX={5} refY={3} orient="auto">
                  <path d="M0,0 L6,3 L0,6 Z" fill="rgba(0,0,0,0.4)" />
                </marker>
              </defs>
              {layout.bars.map((bar) => (
                <GanttBarRect
                  key={`bar-${bar.kind}-${bar.id}`}
                  bar={bar}
                  scaleX={scaleX}
                  scaleY={scaleY}
                  barHeight={barHeight}
                  boxed={boxed}
                  onHoverChange={setHoveredLabel}
                />
              ))}
            </Box>
          </Box>
        </Box>
      )}
    </Box>
  );
}

// Both panes are fixed-height and independently scrollable (see the
// layout note above, on why) — kept in lockstep by mirroring whichever
// one the user is actually scrolling into the other's scrollTop.
// Setting scrollTop to a value it already holds doesn't re-fire
// `scroll`, so this settles in one hop rather than ping-ponging.
function syncScrollTop(from: HTMLDivElement, to: HTMLDivElement | null) {
  if (to) to.scrollTop = from.scrollTop;
}

// Uncommitted local text while typing, so a momentarily-invalid value
// (cleared the box, mid-edit) doesn't get clamped/applied on every
// keystroke — committed on blur/Enter, same as the rest of this app's
// field-commit convention (DenseField.tsx).
function ZoomPercentInput({ value, onCommit }: { value: number; onCommit: (value: number) => void }) {
  const [text, setText] = useState(String(Math.round(value)));
  useEffect(() => setText(String(Math.round(value))), [value]);

  function commit() {
    const parsed = Number(text);
    if (Number.isFinite(parsed) && parsed > 0) onCommit(parsed);
    else setText(String(Math.round(value)));
  }

  return (
    <Box
      component="input"
      type="number"
      value={text}
      onChange={(event: ChangeEvent<HTMLInputElement>) => setText(event.target.value)}
      onBlur={commit}
      onKeyDown={(event: KeyboardEvent<HTMLInputElement>) => {
        if (event.key === "Enter") (event.target as HTMLInputElement).blur();
      }}
      sx={{
        width: 48,
        fontSize: DENSE_FONT_SIZE,
        fontFamily: "inherit",
        border: "1px solid rgba(0,0,0,0.35)",
        borderRadius: "3px",
        px: "4px",
        py: "2px",
      }}
    />
  );
}

function GanttBarRect({
  bar,
  scaleX,
  scaleY,
  barHeight,
  boxed,
  onHoverChange,
}: {
  bar: GanttBar;
  scaleX: number;
  scaleY: number;
  barHeight: number;
  boxed: boolean;
  onHoverChange: (label: string) => void;
}) {
  if (!bar.startDate || !bar.endDate) return null;
  const title = `${bar.label}\n${formatDdMmmYy(bar.startDate)} → ${formatDdMmmYy(bar.endDate)}`;
  // Boxed mode (V1.2's own Project rendering): a Project's own rect grows
  // downward to cover its whole subtree (falling back to its own row when
  // it has no children — subtreeBottomY is null there — so it still
  // reads as a normal-looking bar rather than collapsing to nothing) and
  // switches to a translucent fill/thin border, rather than the solid
  // colour every bar otherwise gets. This same rect keeps doing hover
  // detection either way — in Boxed mode that now naturally means
  // "anywhere in the box", not just the thin strip at the Project's own
  // row, with no extra logic: layout.bars is already rendered in
  // outer-to-inner order (collectRows' own preorder walk), so an inner
  // Project's — or a Task's — rect always paints (and hover-hits) on top
  // of whatever it's nested inside.
  const isBoxedProject = boxed && bar.kind === "project";
  const boxBottomBase = bar.subtreeBottomY ?? bar.y + BAR_HEIGHT;
  return (
    <rect
      x={bar.x * scaleX}
      y={bar.y * scaleY}
      width={bar.width * scaleX}
      height={isBoxedProject ? (boxBottomBase - bar.y) * scaleY : barHeight}
      fill={isBoxedProject ? PROJECT_BOX_FILL : bar.color}
      stroke={isBoxedProject ? EXTENT_LINE_COLOUR : "rgba(0,0,0,0.3)"}
      strokeWidth={isBoxedProject ? 1 : bar.kind === "project" ? 1.5 : 1}
      style={{ cursor: bar.kind === "task" ? "pointer" : "default" }}
      onDoubleClick={bar.kind === "task" ? () => openItemWindow("tasks", bar.id) : undefined}
      onMouseEnter={() => onHoverChange(bar.hoverLabel)}
      onMouseLeave={() => onHoverChange("")}
    >
      <title>{title}</title>
    </rect>
  );
}
