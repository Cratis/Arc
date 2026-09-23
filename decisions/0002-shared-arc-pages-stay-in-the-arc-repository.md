---
id: 0002-shared-arc-pages-stay-in-the-arc-repository
title: Keep Arc's language-agnostic documentation in the Arc repository until a stated trigger
status: proposed
stage: none
class: strategy
reversibility: reversible
applies-to:
  - Documentation/**
---

## Context

Arc ships a C# implementation (this repository) and a Kotlin/Java implementation (Arc.Kotlin), documented as one product. The language-agnostic pages - glossary, HTTP contract, concepts, tutorial, scenarios - live in `Arc/Documentation`, so a JVM contributor fixing a shared page opens a pull request against the C# repository (#2716).

## Decision

The shared pages stay in `Arc/Documentation`. They move to a neutral home - the Documentation repository or a dedicated Arc documentation repository - when either trigger occurs: a JVM contributor is actually blocked on a C#-repository review for a shared-page change, or a third implementation appears.

## Options considered

- **Stay, with a trigger (chosen).** `Documentation/verify-markdown.sh` checks shared pages where they are authored, including compiling every C# snippet against real source; moving them would turn that into "push and wait for the site build".
- **Move the shared pages into the Documentation repository.** Neutral between implementations. Rejected for now: loses the local verification loop, and no contributor has yet been blocked.
- **A dedicated Arc documentation repository both implementations mount into.** Cleanest ownership. Rejected for now: a new repository, CI and review path for a problem that has not yet occurred.

## Default if unanswered

Identical to the decision: the pages stay where they are, and the question is re-asked ad hoc whenever someone notices the asymmetry.

## Timeline and scope

Holds until a trigger occurs. Moving is cheap and reversible: public URLs do not change, because the site maps the product's shared root to `/arc/**` whichever repository supplies it. Out of scope: per-implementation pages, which stay with their implementation.

## Verification

- **Done when:** the shared pages are authored in `Arc/Documentation` and pass its local gate.
- **Verify by:** `./Documentation/verify-markdown.sh`, which validates links, authoring rules and C# snippet compilation for the shared pages.

## Consequences

JVM contributors review shared-page changes in this repository. Reconsidering is triggered by an event, not by preference.
