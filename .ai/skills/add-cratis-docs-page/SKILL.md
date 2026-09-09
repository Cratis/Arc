---
name: add-cratis-docs-page
description: Use this skill whenever creating a new Cratis documentation page, tutorial chapter, guide, explanation, reference page, or documented feature. It chooses the owning repository and Diátaxis purpose, creates the correct Markdown or MDX source, wires product-specific navigation, and verifies both the local source and rendered Astro/Starlight page.
---

# Add a Cratis documentation page

A new page must have one purpose, one authored source, one public route, and one intentional place in navigation. Product docs live in product repositories; site-level cross-product pages live under the Documentation site's authored root.

## 1. Choose purpose and ownership

- Classify the page as one Diátaxis type: tutorial, how-to, explanation, or reference. Use `write-documentation` for the content shape and `writing-cratis-docs` for the tour voice.
- Find the owner using [Editing Cratis Documentation](../../rules/editing-cratis-docs.md). Confirm ambiguous routes in `../Documentation/web/scripts/sync-content.mjs` rather than editing a generated site copy.
- Check neighboring pages before creating a new one. Extend an existing page when the reader's goal is the same; do not fragment one task across shallow pages.

## 2. Create the source

Follow [Documentation Structure and Formatting](../../rules/documentation-structure-and-formatting.md):

- Add `title` and a useful `description`; do not add a body H1.
- Default to `.md` for prose, tables, code, diagrams, images, and titled asides.
- Choose `.mdx` only when Steps, Tabs, Cards, or an established shared component materially improves comprehension.
- Verify framework APIs against source using `writing-correct-examples`.
- Use Mermaid for non-trivial architecture or flow; use `eventmodeling` for EventModeling diagrams.
- Do not create both `<topic>.md[x]` and `<topic>/index.md[x]`; they collide at the public route.

## 3. Wire navigation

For product content:

1. Add the page to the owning `toc.yml`.
2. Read that product's actual `PRODUCTS[].buckets` in `../Documentation/web/scripts/sync-content.mjs`.
3. Add or place the section in the matching product-specific bucket only when needed.

The bucket names are not universal. A missing `toc.yml` entry makes a page unreachable; a missing bucket assignment can leave it misplaced rather than absent. External URLs, `../`, and `/api/` toc targets are dropped, and one-child groups collapse, so inspect generated navigation.

For site-level pages, wire the hand-authored topic in `../Documentation/web/astro.config.mjs` instead.

Use real `.md`/`.mdx` extensions in product-source links. Use clean root-relative public routes for site-level and cross-product links.

## 4. Verify

From the owning product repository:

```bash
./Documentation/verify-markdown.sh
```

Then, when the sibling site checkout is available:

```bash
cd ../Documentation/web
npm run check
```

Require zero dropped/broken toc entries for the new page, zero hard lint errors, and zero broken rendered links attributable to the change. Preview and use `qa-cratis-docs` in light and dark for any visual authoring feature.

Commit in the owning product repository. Change the Documentation repository too only when the new page deliberately requires site-level navigation, a shared component/style, or a redirect.
