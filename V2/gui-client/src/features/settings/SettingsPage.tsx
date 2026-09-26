import { useState } from "react";
import Box from "@mui/material/Box";
import { FieldSelect } from "../../components/DenseField";
import { useDocumentTitle } from "../../lib/useDocumentTitle";
import { useSingletonWindowIdentity } from "../../lib/windowNav";
import {
  getHintMode,
  getNewWindowMode,
  getShowUserNameInTitleMode,
  getUneditableDimmingLevel,
  setHintMode,
  setNewWindowMode,
  setShowUserNameInTitleMode,
  setUneditableDimmingLevel,
  type HintMode,
  type NewWindowMode,
  type ShowUserNameInTitleMode,
  type UneditableDimmingLevel,
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
  const [showUserNameMode, setShowUserNameModeState] = useState<ShowUserNameInTitleMode>(() =>
    getShowUserNameInTitleMode(),
  );
  const [uneditableDimmingLevel, setUneditableDimmingLevelState] = useState<UneditableDimmingLevel>(() =>
    getUneditableDimmingLevel(),
  );

  function handleNewWindowModeChange(mode: NewWindowMode) {
    setNewWindowMode(mode);
    setNewWindowModeState(mode);
  }

  // D1.4-137 — same live-cross-window shape as Hints below (`lib/settings.
  // ts`'s own `useUneditableDimmingLevel`): a `TaskGrid` window's own cell
  // styling updates the moment this changes, in every already-open window,
  // not just the next one.
  function handleUneditableDimmingLevelChange(level: UneditableDimmingLevel) {
    setUneditableDimmingLevel(level);
    setUneditableDimmingLevelState(level);
  }

  // Stage5 — same live-cross-window shape as Hints below (`lib/settings.ts`'s
  // own `useShowUserNameInTitle`), since `useDocumentTitle` re-renders (and
  // re-sets `document.title`) in every already-open window the moment this
  // changes, not just the next window opened.
  function handleShowUserNameModeChange(mode: ShowUserNameInTitleMode) {
    setShowUserNameInTitleMode(mode);
    setShowUserNameModeState(mode);
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
          {/* Stage5 — primarily a testing aid: with several app windows open
              at once, each logged in as a different Person, this puts that
              Person's own name/nickname in the OS taskbar/Alt-Tab title so
              the windows are distinguishable there — otherwise identical.
              Only visible in its effect while running as an installed app
              (`useDocumentTitle`'s own `isAppMode` check) — a plain browser
              tab's title is unaffected either way. */}
          <FieldSelect
            label="Show User name in window title"
            width={140}
            value={showUserNameMode}
            onChange={(value) => handleShowUserNameModeChange(value as ShowUserNameInTitleMode)}
          >
            <option value="On">On</option>
            <option value="Off">Off</option>
          </FieldSelect>
          {/* D1.4-137 — how strongly TaskGrid.tsx's own non-editable cells
              are dimmed (text + urgency-tint background), after two rounds
              of the one fixed amount being reported as wrong. "None" turns
              the whole affordance off — every cell looks the same regardless
              of whether it's actually editable. */}
          <FieldSelect
            label="Uneditable dimming"
            width={140}
            value={uneditableDimmingLevel}
            onChange={(value) => handleUneditableDimmingLevelChange(value as UneditableDimmingLevel)}
          >
            <option value="None">None</option>
            <option value="Low">Low</option>
            <option value="Medium">Medium</option>
            <option value="Max">Max</option>
          </FieldSelect>
        </Box>
      </Box>
    </Box>
  );
}
