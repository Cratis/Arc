---
id: 0003-shared-examples-show-only-shipped-capabilities
title: Show a capability on a shared page only once every backend ships it
status: accepted
stage: implemented
decided: 2026-09-23
decider: Sindre Alstad Wilting
class: product
reversibility: reversible
applies-to:
  - Documentation/**
  - Documentation/client-snippets/**
---

## Context

Shared Arc pages show one example per backend language in selectable tabs. The C# and JVM implementations release independently, so a tab can be true of one implementation's current release and not the other's, and a shared page has no way to say which versions an example applies to (#2717).

## Decision

A capability appears in a shared page's language tabs only once every backend ships it. Until then it is documented on the pages of the implementation that has it, and the shared page, if it mentions the capability at all, says in prose which implementation supports it. Examples carry no per-tab version annotations.

## Options considered

- **Share only what every backend ships (chosen).** Keeps shared pages unconditionally true, needs no convention an author could forget, and is already enforced by tooling: Arc.Kotlin's snippet validator fails when a Kotlin snippet lacks its Java sibling, and the site warns when a shared snippet id lacks any backend's version.
- **A per-tab minimum-version note.** Precise, but repeated in every affected snippet and easy to leave stale once both implementations catch up.
- **A page-level aside stating the assumed versions.** One place to look, but coarse, and silent about which example on the page it concerns.

## Default if unanswered

Authors decide case by case, and the first capability to land in one backend ahead of the other produces a shared tab that is false for the other.

## Timeline and scope

Holds while backends release independently. In scope: shared pages and their snippet roots. Out of scope: each implementation's own pages, which describe their own releases.

## Verification

- **Done when:** a shared snippet id with no version for one backend is reported rather than silently rendered without that tab.
- **Verify by:** the Documentation site sync's `warnOnMissingSnippet` warning for the Arc backend axis, and Arc.Kotlin's `validate-client-snippets.py` Kotlin/Java parity check.

## Consequences

New capabilities reach shared pages later than they reach an implementation's own pages. Shared pages never need version bookkeeping.
