---
id: 0014-prefer-idiomatic-command-decisions-and-state-based-specs
title: Prefer idiomatic command decisions and state-based application specifications
status: accepted
stage: implemented
decided: 2026-10-31
decider: Repository maintainer (direct user instruction via the orchestrating session)
class: product
reversibility: easy
applies-to:
  - Source/DotNET/Chronicle.Testing/**
  - Source/DotNET/Chronicle.CodeAnalysis/**
  - Documentation/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Protected command decisions exposed framework acquisition details to application specifications. Authors were asked to select a decision mode, seed events for a plain model and simulate competing appends to prove a rule. That couples application behavior to the framework's implementation and repeats concurrency proof that belongs in Arc and Chronicle.

This decision follows the maintainer's explicit instructions for Arc #3586. Decision number 0007 was already assigned, so this record uses the next free number, 0014. It supplements [0003](0003-shared-examples-show-only-shipped-capabilities.md): examples must be both shipped and idiomatic.

## Decision

Arc documentation and examples always use and recommend the most idiomatic Cratis way. Advanced or non-idiomatic APIs are documented only as such, never as the default path merely because they remain available.

Command decisions take plain state in `Handle()`. Prefer a plain read-model parameter, then `[LookUpBy]` for a command property key, then a per-stream model for the command route. Only keys no parameter can express use `IDecisionReads` in `Provide()`, returning plain values to `Handle()`. `Handle(IDecisionReads)` and `Handle(DecisionRead<T>)` remain supported advanced forms, not recommended application code. ARCCHR0039 gives Info-level guidance; it does not change runtime protection.

Application specs give state and assert outcomes. They call `Handle(state)` directly or use `CommandScenario<T>` with `Given.ForEventSource(id).ReadModel(state)`. Spec authors never configure decision reads or simulate competing appends. Admission, enrollment, decision tokens and conflict detection still run before a protected plain parameter receives its pinned instance. Arc does not add application test support for non-recommended handler patterns.

Arc and Chronicle's own framework specs prove concurrency handling. `AppendConcurrently` remains public with unchanged behavior, hidden with `[EditorBrowsable(EditorBrowsableState.Never)]`, for a specific, explicit framework-verification need. It is not obsolete and is mentioned only in advanced documentation notes. `UseDecisionReads` is obsolete compatibility API because the scenario selects its read path from command declarations.

## Options considered

- **Plain state and framework-owned concurrency proof (chosen).** Keeps application specs about the domain rule while retaining real production protection.
- **Expose decision-mode setup and require race simulations in application specs.** Repeats framework proof and makes otherwise simple state-dependent rules hard to specify.
- **Remove advanced concurrency APIs.** Unnecessarily breaks supported explicit uses. Keep the API and its behavior, but clearly distinguish it from recommended application testing.

## Default if unanswered

The old guidance would continue to teach framework setup and race simulation as application testing. That is not the accepted direction; this record captures the maintainer's decision instead.

## Timeline and scope

Apply now to Arc #3586 and future documentation and examples. Existing advanced APIs keep their runtime semantics. This decision does not choose Chronicle testing stores, change guard guarantees or introduce support for pinning decision tokens or per-stream state.

## Verification

**Done when:** plain protected pins work, hostile framework specs still reject matching concurrent changes and admission refusals, ARCCHR0039 guides handler signatures without changing runtime behavior, and application guidance requires neither mode setup nor concurrency simulation.

**Verify by:** Chronicle command and reader specs, Chronicle analyzer specs, documentation authoring/link/snippet checks, and CodeScene's changed-code safeguard.

## Consequences

Applications can specify decisions with plain values and use pipeline scenarios only where pipeline behavior matters. Framework maintainers retain advanced hooks to prove guard mechanics. Documentation must label supported but non-recommended alternatives rather than presenting all available APIs as equally idiomatic.
