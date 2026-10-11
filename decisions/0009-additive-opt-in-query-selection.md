---
id: 0009-additive-opt-in-query-selection
title: Additive opt-in query selection
status: accepted
stage: implemented
class: contract
reversibility: costly
decided: 2026-10-11
decider: woksin
applies-to:
  - Source/DotNET/Shared/ModelBoundQueryMethod*.cs
  - Source/DotNET/Shared/QuerySelectionPolicy.cs
  - Source/DotNET/Arc.Core/Queries/ModelBound/**
  - Source/DotNET/Arc.Core.Generators/**
  - Source/DotNET/Arc.Core.CodeAnalysis/**
  - Source/DotNET/Chronicle.CodeAnalysis/**
  - Source/DotNET/Tools/ProxyGenerator/ModelBound/**
  - Source/DotNET/Screenplay/Analysis/Queries/**
  - Source/DotNET/Testing/Queries/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Static factories and upcoming Chronicle self-reducer folds can share a read model with query methods. Excluding names or event-shaped parameters automatically changes existing endpoint sets, while removing `[ReadModel]` changes the public contract. The unreleased opt-out proposal for Arc #3163 does not meet the backward-compatibility requirement.

## Decision

Retain `[ReadModel]` and add its `RequireQueryAttribute` setting, default false. Conventional discovery and diagnostics keep their existing behavior. True selects only eligible methods carrying Arc's method-only, noninherited, nonrepeatable `[Query]`. A marker never switches modes, hides siblings, makes an invalid shape eligible, or permits an owner without `[ReadModel]`. The nearest read-model declaration supplies the setting across partials and inheritance; a direct declaration resets false, and enclosing types do not supply it. Explicit selection adds marker and structural rules but retains each surface's conventional return-shape predicate: marking a valid conventional query does not change whether its return shape is supported. Selection precedes name-only registry lookup; duplicate selected query names are errors in explicit mode. Reflection, metadata loading, compiler generation, diagnostics, proxy emission, Screenplay, and query scenarios apply the same additive selection policy without harmonizing legacy discovery differences.

## Options considered

- **Type-level opt-in with method markers (chosen).** Gives mixed-purpose models a predictable endpoint surface while existing applications remain unchanged.
- **Automatic exclusions for Create, Reduce, event attributes, or event context (rejected).** Changes existing APIs and cannot know the runtime event universe reliably.
- **A method-level NotAQuery opt-out (rejected).** Makes new helpers endpoints unless their authors remember a negative marker.
- **A marker that implicitly changes its owner's mode (rejected).** Adding or removing one marker silently changes sibling endpoints across partial files.
- **Replacing ReadModel or harmonizing all legacy return and accessibility rules (rejected here).** Expands an additive minor feature into a migration and unrelated compatibility changes.

## Default if unanswered

Existing conventional discovery remains the default, with fold-shaped methods potentially exposed as queries. Mixed-purpose models would need to move helpers elsewhere rather than rely on unapproved automatic exclusions.

## Timeline and scope

This policy applies to the additive Arc change approved by woksin on 2026-10-11 and subsequent query selection work. It includes actionable validation of explicit markers and a heuristic Chronicle warning with a previewable whole-type migration. It excludes Chronicle's future static self-reducer implementation, complete fold/query role-conflict validation, and legacy return-shape harmonization. Unmarked Task-returning lifecycle helpers in explicit mode are ordinary helpers, not globally reserved callbacks.

## Verification

- **Done when:** default endpoint sets and diagnostics are unchanged, every explicit-mode reader selects the eligible marked methods that pass its existing conventional return predicate, invalid markers and duplicate selected names fail, and migration documentation explains endpoint preview.
- **Verify by:** run affected runtime, generator, analyzer, proxy, Screenplay, and query-scenario specifications and the repository documentation gates, followed by the parent session's full release gates and independent review. This record accepts the policy; its stage is not a claim that those gates have passed.

## Consequences

Existing applications need no change. A mixed-purpose model can deliberately choose its endpoint surface, including zero queries, without changing direct helper calls. Migration still requires human endpoint review because a return shape cannot prove intention. Chronicle fold semantics and any future global validation require their own implementation and coverage.
