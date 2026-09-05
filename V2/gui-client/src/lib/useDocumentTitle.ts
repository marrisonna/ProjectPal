import { useEffect } from "react";

/**
 * Sets the current window's title. Each Task/Task List window opened via
 * windowNav.ts is a real, separate top-level OS window (not a browser tab),
 * so a distinct document.title per window is what Alt-Tab actually shows
 * for it — otherwise every window falls back to index.html's static
 * "ProjectPal" title.
 */
export function useDocumentTitle(title: string): void {
  useEffect(() => {
    document.title = title;
  }, [title]);
}
