---
id: 0005-keep-shared-arc-pages-with-the-typescript-preview
title: Keep shared Arc pages in Arc after the TypeScript preview appears
status: accepted
stage: implemented
decided: 2026-09-27
decider: Sindre Alstad Wilting (delegated to the maintainer's AI orchestrator session)
class: strategy
reversibility: reversible
supersedes: 0002-shared-arc-pages-stay-in-the-arc-repository
applies-to:
  - Documentation/**
---

## Context

Decision 0002 kept shared Arc pages in `Arc/Documentation` until a contributor was blocked on an Arc-repository review or a third backend appeared (#2716). Arc for TypeScript is now that third implementation, but it is a source preview, not a published npm backend with parity. The trigger has occurred and is reconsidered here for PR #2759. The HTTP contract and shared capability content are verified here against the C# and JVM implementations; TypeScript support is identified separately by its capability reference.

## Decision

Shared Arc pages remain in `Arc/Documentation` despite the arrival of Arc for TypeScript. Reconsider their home if a contributor to a non-C# implementation is actually blocked on an Arc-repository review of a shared-page change, or if Arc for TypeScript ships a published backend package and joins the shared contract verification. Either event prompts a new decision, not an automatic move. Implementation-specific pages stay with their implementations. Decision 0003's rule for shared examples remains in force.

## Options considered

- **Stay and revise the reconsideration triggers (chosen).** Shared contract content stays beside its local verification loop. The TypeScript preview does not yet establish shared wire parity, and no contributor is known to be blocked by the review boundary.
- **Move shared pages to the Documentation repository now.** Neutral ownership, but local verification would become push-and-wait-for-site-build, without a published third backend requiring it.
- **Move shared pages to a dedicated Arc documentation repository now.** Separates ownership, but adds a repository and review path while losing the current local verification loop.

## Default if unanswered

The old third-implementation trigger would remain in force after being met, leaving contributors to infer whether the pages must move. Keeping the pages in place without a new ruling would make that contradiction recur in reviews.

## Timeline and scope

Effective for the TypeScript source preview and subsequent shared-page changes until either new reconsideration trigger occurs. In scope: the home and verification boundary of shared Arc pages, including the HTTP contract and capability content. Out of scope: implementation-specific pages, claims of TypeScript parity, and the shipped-capabilities rule in decision 0003. A published TypeScript backend alone prompts reconsideration only when it joins shared contract verification.

## Verification

- **Done when:** shared pages remain in `Arc/Documentation`, describe the shared contract as verified for C# and JVM without implying TypeScript parity, and implementation-specific pages remain with their implementations.
- **Verify by:** inspect the shared backend landing page, HTTP contract and capability matrix in this tree for their C#/JVM verification boundary and TypeScript preview links; check that implementation-specific documentation remains outside the shared root; then run `./Documentation/verify-markdown.sh` for local authoring, links and snippets.

## Consequences

Shared documentation keeps its immediate local verification loop, at the cost of requiring non-C# contributors to submit shared-page edits to the Arc repository. A review blockage or a published, contract-verified TypeScript backend reopens the ownership choice rather than silently relocating pages.
