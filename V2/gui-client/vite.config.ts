import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";

// https://vite.dev/config/
export default defineConfig({
  server: {
    // Dev-time stand-in for D1.6-4's Caddy routing (/api/* -> rest-api, prefix
    // stripped) — same relative path in dev and prod, and avoids the browser
    // ever making a genuinely cross-origin request (no CORS config needed on
    // the API for this).
    proxy: {
      "/api": {
        target: "http://localhost:8000",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ""),
      },
    },
  },
  plugins: [
    react(),
    // D1.4-5 (Claude/Level1_Implementation/4_GuiClient/Plan.md §3.7): installable,
    // chrome-less "standalone" app window — no separate native shell.
    VitePWA({
      registerType: "autoUpdate",
      manifest: {
        name: "ProjectPal",
        short_name: "ProjectPal",
        description: "ProjectPal V2 — project, task, and resource planning",
        theme_color: "#1e3a5f",
        background_color: "#ffffff",
        display: "standalone",
        icons: [
          {
            // Placeholder icon (public/icon.svg) — swap for real branding later.
            src: "icon.svg",
            sizes: "any",
            type: "image/svg+xml",
            purpose: "any",
          },
        ],
      },
    }),
  ],
});
