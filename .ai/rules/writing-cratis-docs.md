---
applyTo: "**/Documentation/**/*.{md,mdx}"
paths:
  - "**/Documentation/**/*.md"
  - "**/Documentation/**/*.mdx"
---

# Writing Cratis Documentation — tour voice + Starlight authoring

The Cratis docs must **take the reader on a tour, like a teacher** — the way [Marten](https://martendb.io), [Wolverine](https://wolverinefx.net), and [aspire.dev](https://aspire.dev) docs do — **not** state facts like a reference dump. This is the project's strongest qualitative bar and it's been flagged repeatedly. The differentiator of those docs is **pedagogical structure**, not looks. Match it.

## The bar

- **Pain → relief.** Open by naming the friction the reader feels, then reveal the feature as the relief. (Aspire's "without vs. with".)
- **Why before how.** A reader who understands the reasoning handles edge cases the docs don't cover.
- **Active voice, present tense, second person.** "You append the event," not "the event is appended."
- **Be honest about limits.** A "when this is the wrong fit" section builds more trust than omitting it.

## One page = one Diátaxis type

| Type | Reader is… | Reads like |
|---|---|---|
| **Tutorial** | learning by doing | a guided lesson — each step produces a visible result |
| **How-to** | solving a specific problem | a recipe — assume competence, no teaching |
| **Explanation** | trying to understand | a discussion — concepts, trade-offs, *why*, a diagram |
| **Reference** | looking something up | a dictionary — exhaustive, terse, tables/signatures |

Never mix types. A tutorial padded with reference detail overwhelms; a how-to interrupted by concept digressions stops being a recipe.

## The tour-voice checklist (apply to tutorials, getting-started, and explanations)

1. **Open with a concrete scenario** ("a book shows up — let's record that"), not a definition of the tool.
2. **Name the friction first**, then the feature as its relief.
3. **"Let's…" with chronological verbs** (define → append → project → query).
4. **After every code block, explain the invisible** — what happens under the hood and why it matters.
5. **Recap before pivoting** ("this works well for X… however…").
6. **Anticipate the reader's doubt** with an inline aside ("in a real app you'd…").
7. **Show the result** — the output, the resulting read model/JSON — so success is visible.
8. **Organize by workflow**, not alphabetically (especially CLI/command docs).
9. **End every section with a forward link** to the natural next step.

`Chronicle/Documentation/tutorial/*` is the reference voice — read it before writing.

## Use presentation to support the tour

Choose the simplest authoring surface that preserves the reading flow. Use steps for real procedures, tabs for genuine alternatives, asides for meaningful context or risk, and diagrams for non-trivial flows. Do not turn sequential cause-and-effect examples into tabs merely because they use different languages; hiding one side can make the explanation harder to follow.

Full-stack type safety is a differentiator, so show both the backend contract and generated frontend shape when both matter. Use `FullStackTabs` only when each pane remains understandable independently.

The published “Copy Markdown” and AI surfaces preserve raw authored Markdown/MDX. Component imports and JSX therefore reach those consumers too; prefer plain Markdown unless a component adds real teaching value.

The exact Markdown/MDX boundary, aside semantics, component contracts, and import paths live in [Documentation Structure and Formatting](./documentation-structure-and-formatting.md). Do not duplicate or infer that rendering API here.

## Two voices, and connect the products

- **Two voices per area:** the toured/educational layer (tutorial, getting-started, "Understanding…") **and** the terse, exhaustive reference. The narrative pages **link *down*** into the reference; the reference stays a dictionary.
- **Connect at the seams** rather than re-explaining: a command (Arc) appends events (Chronicle); a query reads a projection (Chronicle) rendered by a component (Components). Link to the **glossary** for shared terms instead of redefining them.
- **Coming-from-X bridges** map new concepts to what the reader knows (MediatR, MVC, EF/CRUD, Marten/other event stores).

## Before you call a page done

- Every framework API in a code example is **verified against real source** — see [Writing Correct Code Examples](./writing-correct-examples.md). Readers paste snippets verbatim.
- The product's local documentation gate passes; when available, the sibling Documentation site's full check has 0 hard lint errors and 0 broken rendered links.
- For a visual page, screenshot it in light **and** dark — see the `qa-cratis-docs` skill.

**Study the masters:** the **aspire.dev** docs (Astro Starlight — great CLI docs + tour writing)

→ The mechanical format a page must follow (frontmatter, headings, asides, code fences, file layout): [Documentation Structure & Formatting](./documentation-structure-and-formatting.md). The edit→sync→verify loop and where pages live: [Editing Cratis Documentation](./editing-cratis-docs.md). Site build internals live in the Documentation repo.
