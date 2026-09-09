---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---

# Editing Cratis documentation

Cratis documentation is split across product repositories and aggregated by the sibling `Documentation` repository. Find the authored source before editing; synced product copies under `Documentation/web/src/content/docs/` are disposable build output.

## Find the source of truth

Common routes map as follows:

| Public route | Authored source |
|---|---|
| `/chronicle/**` | `Chronicle/Documentation/**` |
| `/arc/**` | `Arc/Documentation/**` |
| `/components/**` | `Components/Documentation/**` |
| `/chronicle-mcp/**`, `/authproxy/**`, `/cli/**`, `/fundamentals/**`, `/screenplay/**`, `/prologue/**`, `/prompter/**` | The matching product repository's `Documentation/**` |
| `/contributing/**` | The organization `.github` repository (legacy fallback: `GitHubLanding`) |
| Site-level routes such as `/`, `/why-cratis`, `/cratis-stack`, `/glossary`, `/compare-event-sourcing-dotnet`, and `/compare-event-sourcing-jvm` | `Documentation/web/src/content/docs/**` |

The definitive map is `PRODUCTS` in `Documentation/web/scripts/sync-content.mjs`. The site prefers sibling checkouts and falls back to configured submodules.

Never edit a synced subtree under `Documentation/web/src/content/docs/`. Current generated prefixes include `chronicle`, `chronicle-mcp`, `arc`, `components`, `authproxy`, `cli`, `fundamentals`, `contributing`, `architecture`, `screenplay`, `prologue`, and `prompter`. `architecture/` is generated even if Git currently makes it look trackable.

Site-level files authored directly under `web/src/content/docs/` are the exception. Check `PRODUCTS` and `web/.gitignore` when ownership is unclear.

## Edit and verify

From the product repository:

1. Edit the authored `.md` or `.mdx` file.
2. Run the product's local gate: `./Documentation/verify-markdown.sh`.
3. For full rendering, run the site from the sibling checkout:

   ```bash
   cd ../Documentation/web
   npm run check
   ```

4. Preview with `npm run dev` from that same `../Documentation/web` directory and inspect visual changes in light and dark.

The full site check syncs every available product and also runs Chronicle client-doc parity. It can expose an unrelated sibling failure; diagnose and report that separately. Local prose, Markdown, or external-link tools may skip when their executables are absent, so report which checks actually ran.

Restart `npm run dev` after a build/check. The build re-sync can degrade a running dev server, producing 500s or missing table rendering. If a change still appears stale, clear `web/.astro` and `web/node_modules/.astro`, restart, and recheck before blaming the source.

## Add, move, rename, or delete a page

- Product navigation comes from its `toc.yml`; site-level navigation comes from `astro.config.mjs`.
- Product navigation buckets are defined per product in `PRODUCTS[].buckets`. Read the actual names and section lists before changing them.
- Keep exactly one landing for a route. A sibling `<folder>.md[x]` collides with `<folder>/index.md[x]`; the converter moves the directory index to `/overview/`, which can leave it orphaned.
- Update inbound links and `toc.yml` together. For a published route change, inspect the Documentation site's redirect mechanism rather than assuming a source-file move preserves old URLs.
- Watch sync output for dropped toc entries and verify the built sidebar. External, `../`, and `/api/` toc targets are intentionally omitted; single-child groups collapse.

## Links

- Product source links to files keep the real `.md` or `.mdx` extension. The converter removes either extension for the public route.
- Directory links end in `/`.
- Site-level MDX and cross-product links use clean root-relative public routes such as `/arc/backend/commands/`.
- Slugification removes punctuation from path segments (`react.mvvm` becomes `reactmvvm`), so verify hand-authored site-absolute paths against the build.

## Content and rendering

- Match the page's Diátaxis type and the tour voice in [Writing Cratis Documentation](./writing-cratis-docs.md).
- Verify framework APIs against source using [Writing Correct Code Examples](./writing-correct-examples.md).
- Follow [Documentation Structure and Formatting](./documentation-structure-and-formatting.md) for Markdown/MDX boundaries, asides, components, navigation behavior, and gates.

Commit in the repository that owns the authored source. Touch the Documentation repository only when the task deliberately changes site-level content, navigation composition, components, styling, redirects, or build behavior.
