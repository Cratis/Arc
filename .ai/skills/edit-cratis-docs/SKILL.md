---
name: edit-cratis-docs
description: Use this skill whenever changing, correcting, moving, renaming, or deleting Cratis documentation, including prose, examples, links, callouts, diagrams, and navigation. It finds the authored product or site source, preserves public routes, applies the Astro/Starlight contract, and runs the appropriate local and rendered checks. For a brand-new page use add-cratis-docs-page; for a visual/rendering failure use qa-cratis-docs as well.
---

# Edit a Cratis documentation page

Cratis product documentation is authored in each product repository and synchronized into the sibling Documentation site's generated trees. Read [Editing Cratis Documentation](../../rules/editing-cratis-docs.md) before touching a URL-backed page; it owns the complete source map and generated-content boundaries.

## 1. Locate the authored source

- Map the public route through `PRODUCTS` in `../Documentation/web/scripts/sync-content.mjs` when ownership is not obvious.
- Never edit a synchronized product subtree under `../Documentation/web/src/content/docs/`, including the generated `architecture/` tree.
- Read the whole page plus its `toc.yml`, inbound links, and immediate neighboring pages. A local wording change can still alter a public anchor or duplicate a landing route.

For move/rename/delete work, search inbound links and check whether the public route needs a Documentation-site redirect. Do not assume a source move preserves the URL.

## 2. Make the smallest coherent improvement

- Preserve the page's single Diátaxis purpose and use the tour voice from `writing-cratis-docs`.
- Verify every framework API against source using `writing-correct-examples`; another docs page is not evidence.
- Follow `documentation-structure-and-formatting` as the single rendering authority. It covers frontmatter, headings, `.md` versus `.mdx`, the closed aside set, code metadata, tables, diagrams, components, icons, links, and navigation behavior.
- Prefer semantic structure over decoration. Keep sequential examples visible when their order teaches cause and effect; use tabs only for true alternatives.
- Avoid unrelated mass modernization. Convert legacy alerts or add missing descriptions when already touching the page, but do not churn dozens of otherwise-correct files for visual consistency alone.

## 3. Verify locally

From the product repository:

```bash
./Documentation/verify-markdown.sh
```

This checks Markdown and MDX linting, Starlight authoring hazards, landing collisions, and links. Fix all failures introduced by the change.

## 4. Verify the rendered site

When the sibling Documentation checkout is available:

```bash
cd ../Documentation/web
npm run check
```

The full check syncs all available products, builds Astro, runs Chronicle client-doc parity, linting, and rendered-link checks. Report unrelated sibling failures separately; do not waive failures from the edited page. Some optional local tools skip when unavailable, so say what actually ran.

For callouts, diagrams, tabs, cards, component changes, or layout-sensitive prose, use `qa-cratis-docs` and inspect light and dark screenshots. Restart `npm run dev` after a build/check before trusting the preview.

## 5. Stop at the requested scope

Commit only in the repository that owns the authored change. Do not modify generated site copies, publish, push, or change redirects/site styling unless the user requested that scope.
