---
name: qa-cratis-docs
description: Use this skill whenever Cratis documentation must be visually reviewed or a docs render/gate problem must be diagnosed: screenshots, light/dark appearance, raw table pipes, unstyled callouts, literal MDX imports, missing sidebar pages, broken Astro builds, stale previews, Mermaid failures, layout shift, or flicker. It verifies the authored source, synced output, rendered HTML, and final pixels without guessing.
---

# Diagnose and visually verify Cratis documentation

Product docs render through the sibling `Documentation/web` Astro Starlight site. A source file can pass Markdown lint while silently rendering the wrong HTML, so inspect each boundary: authored source → synced source → built HTML → screenshot.

## 1. Reproduce from a fresh site state

From the product repository:

```bash
./Documentation/verify-markdown.sh
cd ../Documentation/web
npm run check
```

The full check requires the Documentation checkout and available sibling products. Separate failures caused by unrelated sibling content from failures in the page under review.

After a build/check, restart the preview before trusting it:

```bash
cd ../Documentation/web
npm run dev
```

A build re-sync can degrade an already-running dev server. If output remains stale or partial, stop the server, remove `.astro` and `node_modules/.astro`, then restart. Do not interpret a degraded preview as a source defect.

`web/src/generated/topics.json` exists only after sync; use it with site-level slugs when building a page list.

## 2. Diagnose the symptom

| Symptom | Check first |
|---|---|
| Raw `|` table text | Confirm `remarkGfm` remains in `astro.config.mjs`; restart a degraded dev server |
| Untitled/unstyled callout | The only directive variants are `note`, `tip`, `caution`, `danger`; an unknown name silently becomes a plain `<div>` |
| Literal `import …` text or inert component tag | JSX was authored in `.md`; rename/wire it deliberately as `.mdx` |
| Page absent from sidebar | Check its `toc.yml`, sync's dropped-entry count, product-specific bucket, and one-child-group collapse |
| Real landing only at `/overview/` | Look for a `<folder>.md[x]` plus `<folder>/index.md[x]` route collision |
| Empty icon | Verify the installed Starlight icon name; several component icon failures are silent (`seti:windows`, not `windows`) |
| Mermaid source instead of SVG | Check diagram syntax and whether build-time pre-render fell back; inspect logs and built HTML |
| Page change did not appear | Confirm you edited the product source rather than a generated `web/src/content/docs/<product>/` copy |
| 500s, missing tables, or partial pages | Restart dev after build/check; clear Astro caches if needed |
| Link works in source but not site | Inspect the converted route, punctuation-stripped slug, and generated HTML |

Inspect the built page directly when useful. A real aside has `starlight-aside--<variant>`; a pre-rendered diagram has a Mermaid SVG marker; tables produce `<table>`.

## 3. Capture light and dark

The committed screenshot script drives system Chrome through CDP and waits for client rendering:

```bash
cd ../Documentation/web
node scripts/screenshot.mjs http://localhost:4321/arc/example/ /tmp/example-dark.png dark
node scripts/screenshot.mjs http://localhost:4321/arc/example/ /tmp/example-light.png light
```

Read both PNGs. Crop with the installed `sharp` package when the relevant section is small:

```bash
node -e "require('sharp')('/tmp/example-dark.png').extract({left:300,top:600,width:900,height:500}).resize({width:1400}).toFile('/tmp/example-crop.png')"
```

The script uses the fixed profile `/tmp/cratis-screenshot-9222`. Delete that directory before launch for a cold profile; reuse it for a warm comparison. Run CDP scripts serially because they share a fixed debug port.

Chrome is a system prerequisite, resolved from `CHROME_PATH` or known install locations; it is not an npm dependency. The Astro dev toolbar in full-page captures is a development-only overlay.

## 4. Visual checklist

- Asides use the correct semantic severity, display the custom title, and contain nested code/tables without overflow.
- H2 headings remain when navigation or stable anchors need them.
- Mermaid diagrams are responsive, themed, legible, and present without a client-side pop.
- GFM tables render as tables and remain keyboard/viewport accessible.
- Code blocks are dedented, titled only when useful, and visually emphasize the intended lines.
- Tabs, steps, cards, and shared components have no raw JSX, missing slots, overflow, or empty icons.
- Light and dark themes both preserve contrast and hierarchy.
- Rich presentation improves comprehension rather than merely adding decoration.

## 5. Measure layout shift when screenshots are not enough

For flicker, twitch, or scroll-restoration defects:

- Inject a buffered `layout-shift` `PerformanceObserver` with `Page.addScriptToEvaluateOnNewDocument`.
- Sample `document.documentElement` height during navigation.
- Compare a cold Chrome profile with a warm reuse.
- For restoration, scroll, reload, and sample `window.scrollY`; landing short usually means content above rendered late.
- Treat font loading, client-side rendering, and Mermaid fallback as separate hypotheses.

Report the source, synced-output, HTML, and screenshot evidence separately. A green build is not visual proof.
