import type { CSSProperties, ReactNode } from "react";
import Box from "@mui/material/Box";

// Dense, WinForms-like field controls matching the Claude Design mockup
// ("Task Detail Compact Mockups.dc.html", option 1a) pixel-for-pixel —
// V1.2's combo/text boxes sit close to the text on all sides, not padded
// like a Material text field, so these render plain native <select>/<input>
// elements in a styled box rather than MUI's TextField/Select (Q1.4-17).
const BORDER = "1px solid rgba(0,0,0,0.35)";

const NATIVE_RESET: CSSProperties = {
  border: "none",
  outline: "none",
  background: "transparent",
  font: "inherit",
  color: "inherit",
  width: "100%",
  height: "100%",
  padding: 0,
};

// lineHeight fixed at 14px (rather than left to the font's own metrics) so
// every label's rendered height is a known, exact value — needed by
// TaskDetailPage.tsx's Resources listbox height, which is computed from
// this plus other fields' fixed pixel sizes, not measured empirically.
export function FieldLabel({ children }: { children: ReactNode }) {
  return <Box sx={{ fontSize: 11, lineHeight: "14px", color: "rgba(0,0,0,0.6)" }}>{children}</Box>;
}

function FieldShell({
  label,
  width,
  flex,
  justifyContent,
  children,
}: {
  label: string;
  width?: number | string;
  flex?: number | string;
  justifyContent?: string;
  children: ReactNode;
}) {
  return (
    <Box sx={{ width, flex, minWidth: 0, display: "flex", flexDirection: "column", gap: "2px" }}>
      <FieldLabel>{label}</FieldLabel>
      <Box
        sx={{
          height: 22,
          px: "5px",
          border: BORDER,
          borderRadius: "3px",
          display: "flex",
          alignItems: "center",
          justifyContent,
          fontSize: 12,
          color: "rgba(0,0,0,0.87)",
          bgcolor: "#fff",
          boxSizing: "border-box",
        }}
      >
        {children}
      </Box>
    </Box>
  );
}

export function FieldSelect({
  label,
  width,
  flex,
  value,
  onChange,
  children,
}: {
  label: string;
  width?: number | string;
  flex?: number | string;
  value: string | number;
  onChange: (value: string) => void;
  children: ReactNode;
}) {
  return (
    <FieldShell label={label} width={width} flex={flex}>
      <select value={value} onChange={(e) => onChange(e.target.value)} style={NATIVE_RESET}>
        {children}
      </select>
    </FieldShell>
  );
}

export function FieldInput({
  label,
  width,
  flex,
  type = "text",
  value,
  onChange,
  readOnly,
  center,
}: {
  label: string;
  width?: number | string;
  flex?: number | string;
  type?: string;
  value: string | number;
  onChange?: (value: string) => void;
  readOnly?: boolean;
  center?: boolean;
}) {
  return (
    <FieldShell label={label} width={width} flex={flex} justifyContent={center ? "center" : undefined}>
      <input
        type={type}
        value={value}
        readOnly={readOnly}
        onChange={onChange ? (e) => onChange(e.target.value) : undefined}
        style={{ ...NATIVE_RESET, textAlign: center ? "center" : "left" }}
      />
    </FieldShell>
  );
}

export function FieldStatic({
  label,
  width,
  flex,
  muted,
  fontSize,
  children,
}: {
  label: string;
  width?: number | string;
  flex?: number | string;
  muted?: boolean;
  fontSize?: number;
  children: ReactNode;
}) {
  return (
    <FieldShell label={label} width={width} flex={flex}>
      <Box
        sx={{
          color: muted ? "rgba(0,0,0,0.4)" : "inherit",
          whiteSpace: "nowrap",
          overflow: "hidden",
          textOverflow: "ellipsis",
          fontSize,
        }}
      >
        {children}
      </Box>
    </FieldShell>
  );
}

export function FieldTextArea({
  label,
  minHeight = 60,
  value,
  onChange,
}: {
  label: string;
  minHeight?: number;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", gap: "4px" }}>
      <FieldLabel>{label}</FieldLabel>
      <Box
        component="textarea"
        value={value}
        onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => onChange(e.target.value)}
        sx={{
          minHeight,
          p: "4px 5px",
          border: BORDER,
          borderRadius: "3px",
          fontSize: 12,
          lineHeight: 1.4,
          fontFamily: "inherit",
          resize: "vertical",
          width: "100%",
          boxSizing: "border-box",
        }}
      />
    </Box>
  );
}

// Requested Start/End Date: a styled box showing our own "dd-Mmm-yy" text,
// with a real, fully transparent <input type="date"> stacked exactly on top
// via position:absolute + opacity:0 — a native date input can't render a
// custom text format itself, so this is the only way to show "09-Feb-26"
// while keeping a genuinely clickable, real date-picker control underneath.
export function DateField({
  label,
  width,
  value,
  display,
  onChange,
}: {
  label: string;
  width?: number | string;
  value: string;
  display: string;
  onChange: (value: string) => void;
}) {
  return (
    <FieldShell label={label} width={width}>
      <Box sx={{ position: "relative", width: "100%", height: "100%", display: "flex", alignItems: "center", justifyContent: "space-between", gap: "4px" }}>
        <Box component="span" sx={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
          {display}
        </Box>
        <Box component="span" sx={{ fontSize: 10, opacity: 0.55, flexShrink: 0 }}>
          📅
        </Box>
        <input
          type="date"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          style={{ position: "absolute", inset: 0, width: "100%", height: "100%", opacity: 0, margin: 0, padding: 0, border: 0, cursor: "pointer" }}
        />
      </Box>
    </FieldShell>
  );
}

export function DenseButton({
  children,
  variant = "outlined",
  onClick,
  disabled,
}: {
  children: ReactNode;
  variant?: "outlined" | "filled";
  onClick?: () => void;
  disabled?: boolean;
}) {
  return (
    <Box
      component="button"
      onClick={onClick}
      disabled={disabled}
      sx={{
        height: 22,
        px: "12px",
        borderRadius: "3px",
        fontSize: 12,
        fontWeight: 600,
        letterSpacing: "0.3px",
        display: "flex",
        alignItems: "center",
        cursor: disabled ? "default" : "pointer",
        opacity: disabled ? 0.5 : 1,
        border: variant === "outlined" ? "1px solid rgba(0,0,0,0.23)" : "1px solid transparent",
        bgcolor: variant === "filled" ? "primary.main" : "rgba(0,0,0,0.04)",
        color: variant === "filled" ? "primary.contrastText" : "rgba(0,0,0,0.7)",
        fontFamily: "inherit",
        boxShadow: "0 1px 1px rgba(0,0,0,0.08)",
        "&:hover": disabled
          ? undefined
          : {
              bgcolor: variant === "filled" ? "primary.dark" : "rgba(0,0,0,0.08)",
            },
        "&:active": disabled
          ? undefined
          : {
              boxShadow: "inset 0 1px 2px rgba(0,0,0,0.15)",
            },
      }}
    >
      {children}
    </Box>
  );
}
