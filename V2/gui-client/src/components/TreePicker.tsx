import { useMemo, useState } from "react";
import Box from "@mui/material/Box";

// V1.2's Project/Component tree picker (V1.2/Apps/ProjectPal/ProjectPal/
// Tasks/TaskDetail.cs's buttonComponents_Click/buttonProjects_Click,
// treeViewSelector): one shared TreeView control, repopulated/repositioned
// for whichever field's "+" was clicked, showing the full hierarchy so the
// user can navigate down to the item to assign rather than picking from a
// flat list. V2 doesn't need V1.2's single-shared-control trick (that only
// existed to avoid a second WinForms TreeView) — this is generic over any
// {id, name, parentId}[] list, so DenseField.tsx's FieldTreePicker just
// renders one independent instance per field.
export interface TreeItem {
  id: number;
  name: string;
  parentId: number | null;
}

function buildChildrenMap(items: TreeItem[]): Map<number | null, TreeItem[]> {
  const map = new Map<number | null, TreeItem[]>();
  for (const item of items) {
    const list = map.get(item.parentId);
    if (list) list.push(item);
    else map.set(item.parentId, [item]);
  }
  for (const list of map.values()) list.sort((a, b) => a.name.localeCompare(b.name));
  return map;
}

/**
 * Full ancestor-to-leaf path for the currently selected item (e.g.
 * "Applications - Other › Intex"), for FieldTreePicker's display box —
 * V1.2 showed a similar breadcrumb (Component.cs/Project.cs's FullName),
 * though not in this exact "›"-joined form; a plain breadcrumb reads more
 * clearly than V1.2's own nested "name => [parent => [...]]" string.
 */
export function buildBreadcrumb(items: TreeItem[], selectedId: number | null): string {
  if (selectedId == null) return "";
  const byId = new Map(items.map((i) => [i.id, i]));
  const parts: string[] = [];
  let current = byId.get(selectedId);
  while (current) {
    parts.unshift(current.name);
    current = current.parentId != null ? byId.get(current.parentId) : undefined;
  }
  return parts.join(" › ");
}

function TreeNodeRow({
  item,
  depth,
  childrenMap,
  expanded,
  onToggle,
  selectedId,
  onSelect,
}: {
  item: TreeItem;
  depth: number;
  childrenMap: Map<number | null, TreeItem[]>;
  expanded: Set<number>;
  onToggle: (id: number) => void;
  selectedId: number | null;
  onSelect: (id: number) => void;
}) {
  const children = childrenMap.get(item.id) ?? [];
  const hasChildren = children.length > 0;
  const isExpanded = expanded.has(item.id);
  return (
    <>
      <Box
        onClick={() => onSelect(item.id)}
        sx={{
          display: "flex",
          alignItems: "center",
          gap: "4px",
          pl: `${depth * 14 + 6}px`,
          pr: "6px",
          py: "3px",
          fontSize: 11,
          cursor: "pointer",
          bgcolor: item.id === selectedId ? "rgba(30,58,95,0.08)" : "transparent",
          "&:hover": { bgcolor: "rgba(0,0,0,0.04)" },
        }}
      >
        {hasChildren ? (
          <Box
            component="span"
            onClick={(e) => {
              e.stopPropagation();
              onToggle(item.id);
            }}
            sx={{ width: 12, flexShrink: 0, fontSize: 9, textAlign: "center", userSelect: "none" }}
          >
            {isExpanded ? "▾" : "▸"}
          </Box>
        ) : (
          <Box component="span" sx={{ width: 12, flexShrink: 0 }} />
        )}
        <Box component="span" sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
          {item.name}
        </Box>
      </Box>
      {hasChildren &&
        isExpanded &&
        children.map((child) => (
          <TreeNodeRow
            key={child.id}
            item={child}
            depth={depth + 1}
            childrenMap={childrenMap}
            expanded={expanded}
            onToggle={onToggle}
            selectedId={selectedId}
            onSelect={onSelect}
          />
        ))}
    </>
  );
}

export function TreePicker({
  items,
  selectedId,
  onSelect,
  allowNone,
}: {
  items: TreeItem[];
  selectedId: number | null;
  onSelect: (id: number | null) => void;
  /** Component's Task field can be cleared; Project's can't (always required). */
  allowNone?: boolean;
}) {
  const childrenMap = useMemo(() => buildChildrenMap(items), [items]);
  const topLevel = childrenMap.get(null) ?? [];

  // Auto-expand down to the current selection, computed once at mount —
  // this component is only ever mounted while the picker is open (see
  // FieldTreePicker), so a fresh mount always means "just opened."
  const [expanded, setExpanded] = useState<Set<number>>(() => {
    const byId = new Map(items.map((i) => [i.id, i]));
    const set = new Set<number>();
    let current = selectedId != null ? byId.get(selectedId) : undefined;
    current = current?.parentId != null ? byId.get(current.parentId) : undefined;
    while (current) {
      set.add(current.id);
      current = current.parentId != null ? byId.get(current.parentId) : undefined;
    }
    return set;
  });

  function toggle(id: number) {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  return (
    <Box
      sx={{
        position: "absolute",
        top: "100%",
        left: 0,
        right: 0,
        mt: "2px",
        maxHeight: 220,
        overflowY: "auto",
        bgcolor: "#fff",
        border: "1px solid rgba(0,0,0,0.25)",
        borderRadius: "4px",
        boxShadow: "0 4px 12px rgba(0,0,0,0.18)",
        zIndex: 10,
        py: "4px",
      }}
    >
      {allowNone && (
        <Box
          onClick={() => onSelect(null)}
          sx={{
            display: "flex",
            alignItems: "center",
            pl: "18px",
            py: "3px",
            fontSize: 11,
            fontStyle: "italic",
            color: "rgba(0,0,0,0.5)",
            cursor: "pointer",
            bgcolor: selectedId == null ? "rgba(30,58,95,0.08)" : "transparent",
            "&:hover": { bgcolor: "rgba(0,0,0,0.04)" },
          }}
        >
          (none)
        </Box>
      )}
      {topLevel.map((item) => (
        <TreeNodeRow
          key={item.id}
          item={item}
          depth={0}
          childrenMap={childrenMap}
          expanded={expanded}
          onToggle={toggle}
          selectedId={selectedId}
          onSelect={onSelect}
        />
      ))}
    </Box>
  );
}
