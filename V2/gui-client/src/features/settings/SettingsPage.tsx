import { useState } from "react";
import Box from "@mui/material/Box";
import { FieldSelect } from "../../components/DenseField";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { useSingletonWindowIdentity } from "../../lib/windowNav";
import {
  getHintMode,
  getNewWindowMode,
  setHintMode,
  setNewWindowMode,
  type HintMode,
  type NewWindowMode,
} from "../../lib/settings";

// D1.4-122 — a small, always-the-same-instance popup window (opened via
// AppShell.tsx's own nav bar, beside Search), for preferences that live in
// this browser (`lib/settings.ts`'s `localStorage` wrapper) rather than the
// server: they're specific to this Person, this machine, and this browser
// profile, and never sent to or read from the database. Every control here
// saves immediately on change — unlike every server-backed Detail screen
// elsewhere in this app, there's no Save button, since there's nothing
// pending to discard by just closing the window.
export function SettingsPage() {
  useSingletonWindowIdentity("settings");
  useDocumentTitle("Settings");

  // Read once, from `localStorage`, as this window's own starting value —
  // not re-read afterward, since this window is the only place that can
  // ever change it (there's no cross-window sync to listen for here, unlike
  // this app's own genuine live-data sync, `lib/liveSync.ts`).
  const [newWindowMode, setNewWindowModeState] = useState<NewWindowMode>(() => getNewWindowMode());
  const [hintMode, setHintModeState] = useState<HintMode>(() => getHintMode());

  function handleNewWindowModeChange(mode: NewWindowMode) {
    setNewWindowMode(mode);
    setNewWindowModeState(mode);
  }

  // D1.4-124 — unlike New Window above, this one is watched live by every
  // *other* open window's own `useHintsEnabled` (`lib/settings.ts`, via the
  // native `storage` event) — a change here takes effect immediately in
  // whatever windows are already open, not just the next one opened.
  function handleHintModeChange(mode: HintMode) {
    setHintMode(mode);
    setHintModeState(mode);
  }

  return (
    <Box sx={{ p: "6px", boxSizing: "border-box" }}>
      <Box sx={{ width: 340, mx: "auto" }}>
        <Box
          sx={{
            bgcolor: "#fff",
            border: "1px solid rgba(0,0,0,0.08)",
            borderRadius: "8px",
            boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
            p: "12px",
            display: "flex",
            flexDirection: "column",
            gap: "10px",
          }}
        >
          <Box sx={{ fontSize: 14, fontWeight: 600 }}>Settings</Box>
          {/* Whether opening another window (a Task, a Project, Search,
              this very Settings window next time, ...) creates a genuine
              separate browser window or a new tab in the same one —
              windowNav.ts's own openNamedWindow reads this at the moment
              each one opens. */}
          <FieldSelect
            label="New Window"
            width={140}
            value={newWindowMode}
            onChange={(value) => handleNewWindowModeChange(value as NewWindowMode)}
          >
            <option value="Window">Window</option>
            <option value="Tab">Tab</option>
          </FieldSelect>
          {/* D1.4-124 — whether every interactive element in the app (a
              double-clickable row, a Ctrl-draggable badge, an editable
              cell, ...) shows a tooltip naming its own available gesture(s)
              and what each does. Applies immediately to every already-open
              window, not just future ones (lib/settings.ts's own
              useHintsEnabled). */}
          <FieldSelect
            label="Hints"
            width={140}
            value={hintMode}
            onChange={(value) => handleHintModeChange(value as HintMode)}
          >
            <option value="On">On</option>
            <option value="Off">Off</option>
          </FieldSelect>
        </Box>
      </Box>
    </Box>
  );
}
