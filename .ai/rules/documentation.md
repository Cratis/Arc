---
applyTo: "Documentation/**/*.md"
---

# How to Write Documentation

Documentation exists for one audience: **developers who need to use the framework** — not the team that built it. Write from the reader's perspective. They want to know *what this does*, *why they should care*, and *how to use it* — in that order.

Every page should answer: "If I were a developer encountering this concept for the first time, what would I need to understand to use it correctly?"

## General

- Documentation is split across product repositories — each product owns its `Documentation/` folder.
- The published site is built and aggregated using [Astro](https://astro.build/) with [Starlight](https://starlight.astro.build/) (no more DocFX).
- Use [Markdown](https://www.markdownguide.org/) with [GitHub Flavored Markdown](https://github.github.com/gfm/) for `.md` files.
- Author rich pages as `.mdx` to unlock Starlight components (`<Steps>`, `<Tabs>`, `<Aside>`) and custom components (`<FullStackTabs>`, `<TopicHero>`).
- Use [Mermaid](https://mermaid-js.github.io/mermaid/#/) for diagrams — architecture flows, state transitions, and sequence diagrams are pre-rendered to SVG at build time.
- Follow the [Astro Starlight](https://starlight.astro.build/) authoring guidelines.

## Structure

- Documentation lives in each **product's own repo** under that repo's `Documentation/` folder — not centralized. The site aggregates them.
- Every folder must have its own `toc.yml` for navigation within that product.
- Every folder must have an `index.md` as a landing page.
- When linking to a folder in `toc.yml`, link to the folder's `toc.yml`, not to `index.md`.
- The site regroupes toc entries into Diátaxis **buckets** (Get started / Guides / Understand / Reference) — this ordering is enforced by `sync-content.mjs` in the Documentation repo.

## Writing Style

The project's voice is **direct, practical, and opinionated** — like a teacher taking readers on a tour, not a manual dump. Write like an experienced colleague explaining something to a capable developer — confident but never condescending.

- **Active voice, present tense.** "Chronicle appends the event" not "The event is appended by Chronicle."
- **Emphasize *why* before *how*.** The reason behind a design choice is more valuable than the steps to implement it. A developer who understands the *why* can handle edge cases the docs don't cover.
- **Don't document the obvious.** If the API is self-explanatory, a code example is enough. Save prose for concepts that are genuinely non-obvious or where the reasoning is important.
- **Open with a concrete scenario**, not a definition of the tool. Name the friction first, then the feature as its relief.
- Use headings, lists, and code blocks to organize content — dense paragraphs lose readers.
- Use consistent terminology throughout. If it's called an "event source" in one place, don't call it an "event stream" elsewhere.
- Focus on public APIs and features — not internal implementation.
- Do not document third-party libraries.
- **Show the result** — after code blocks, explain what happens under the hood and show the output so success is visible.

## Frontmatter

Pages should include YAML frontmatter:

```yaml
---
title: Your page title           # required; becomes the page H1
description: One sentence ...     # strongly recommended; SEO/meta
sidebar:                         # optional: control nav ordering
  order: 2
  badge: { text: New, variant: tip }
---
```

**Do NOT put an H1 in the page body** — the title frontmatter becomes the H1. Start the body at `##` (H2). Starlight shows only H2 in the "On this page" TOC.

## Code Examples

- Prefer `record` types for data structures (events, commands, read models) — this matches the actual codebase.
- When specifying `[EventType]`, never add an explicit name argument — just `[EventType]`.
- Never include verbatim code from the repository — APIs may change. Write purpose-built examples.
- Every code example must be complete and correct — no pseudo-code, no `// ...` elisions that leave the reader guessing.
- **Tag all code blocks with the language** for syntax highlighting: ` ```csharp `, ` ```tsx `, ` ```bash `, etc.
- **Verify every framework API in code examples against real source** — readers paste snippets verbatim.
