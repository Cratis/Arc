---
name: write-documentation
description: Use this skill whenever drafting or substantially restructuring Cratis documentation content. It selects one Diátaxis purpose, audience, reader goal, teaching flow, and evidence standard. Use add-cratis-docs-page for placement/navigation, edit-cratis-docs for an existing source file, and qa-cratis-docs for rendering or visual diagnosis.
---

# Write Cratis documentation

Write for a developer using the framework, not the team that built it. Every page should make one reader goal easier and belong to exactly one Diátaxis type.

## Choose the page type

| Type | Reader need | Shape |
|---|---|---|
| Tutorial | Learn by doing | A guided lesson whose steps produce visible results |
| How-to | Solve a specific problem | Goal, prerequisites, focused steps, completion check |
| Reference | Look up exact behavior | Exhaustive, terse tables, signatures, and contracts |
| Explanation | Understand why | Concepts, trade-offs, architecture, and diagrams; no procedural recipe |

Do not mix types. Navigation bucket names are product-specific and do not determine the page's purpose.

## Establish the writing contract

Before drafting, determine from the request and repository context:

- target audience;
- the reader's concrete goal or question;
- what the page includes and deliberately excludes;
- prerequisite pages and the natural next page;
- the public behavior and source evidence the examples require.

Ask only when those choices cannot be resolved from the existing corpus or source.

## Build the narrative

- Lead with the reader's friction and the relief the feature provides.
- Use active voice, present tense, second person, and American English.
- Organize tutorials and explanations chronologically: define → perform → observe → verify.
- Explain invisible behavior after code blocks: what the framework discovers, generates, validates, appends, or subscribes to.
- Show the visible result so the reader can tell whether they succeeded.
- State limits and wrong-fit cases directly.
- End with a useful recap or next step, not a generic “see also” dump.

Reference pages are the exception to the tour cadence: keep them concise and exhaustive, while linking from educational pages into them.

## Write trustworthy examples

- Verify every framework type, attribute, method, overload, prop, import, and extension receiver against current source. Follow `writing-correct-examples`.
- Invented domain names are fine; invented framework APIs are not.
- Use short purpose-built examples only when they remain complete and verifiable. Embed longer real samples from compiled/tested source when available.
- Do not use pseudo-code or `// ...` omissions that make a pasted example fail.
- Use argument-free `[EventType]` for new events. `generation:` or a legacy identifier is valid only when documenting evolution of an existing stored-event contract.
- Show backend and generated frontend shapes when both matter, but keep causal examples sequential. Tabs are for alternatives, not for hiding half of an explanation.

## Choose presentation deliberately

Follow `documentation-structure-and-formatting` as the single authority for frontmatter, Markdown versus MDX, asides, diagrams, code metadata, components, links, navigation, and verification. Presentation reinforces meaning; it does not replace it.

The AI and “Copy Markdown” surfaces preserve raw authored content. Prefer plain Markdown unless an MDX component creates a real teaching advantage.

## Quality check

Before handing the draft to the edit/add workflow, confirm:

- one clear Diátaxis type and reader goal;
- accurate, source-verified APIs;
- complete examples and visible outcomes;
- meaningful `title` and `description`;
- sentence-case headings and descriptive links;
- explicit limitations where they matter;
- one natural next step;
- a single trailing newline.
