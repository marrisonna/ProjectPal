import react from "@vitejs/plugin-react";
import { defineConfig, type Plugin } from "vite";
import { VitePWA } from "vite-plugin-pwa";
import branding from "./branding.json" with { type: "json" };

// Injects branding.json's values into index.html's <head> at build/dev time,
// so that file never needs a hardcoded value of its own to keep in sync.
function brandIndexHtml(): Plugin {
  return {
    name: "brand-index-html",
    transformIndexHtml(html) {
      return html
        .replace(/<title>.*<\/title>/, `<title>${branding.appName}</title>`)
        .replace(
          /<link rel="icon"[^>]*\/>/,
          `<link rel="icon" type="image/svg+xml" href="${branding.faviconPath}" />`,
        )
        .replace(
          /<meta name="theme-color"[^>]*\/>/,
          `<meta name="theme-color" content="${branding.primaryColor}" />`,
        );
    },
  };
}

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
    brandIndexHtml(),
    // D1.4-5 (Claude/Level1_Implementation/4_GuiClient/Plan.md §3.7): installable,
    // chrome-less "standalone" app window — no separate native shell.
    VitePWA({
      registerType: "autoUpdate",
      manifest: {
        name: branding.appName,
        short_name: branding.shortName,
        description: "ProjectPal V2 — project, task, and resource planning",
        theme_color: branding.primaryColor,
        background_color: branding.backgroundColor,
        display: "standalone",
        icons: [
          {
            src: branding.logoPath,
            sizes: "any",
            type: "image/svg+xml",
            purpose: "any",
          },
        ],
      },
    }),
  ],
});
