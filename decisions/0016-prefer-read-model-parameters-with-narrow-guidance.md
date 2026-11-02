---
id: 0016-prefer-read-model-parameters-with-narrow-guidance
title: Prefer read-model parameters with narrow informational guidance
status: accepted
stage: none
decided: 2026-11-02
decider: Sindre Alstad Wilting (delegated to the maintainer's AI orchestrator session)
class: product
reversibility: reversible
supersedes: 0015-handle-dependencies-are-the-authors-choice
applies-to:
  - Source/DotNET/Chronicle.CodeAnalysis/**
  - Documentation/backend/csharp/chronicle/**
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

[0015](0015-handle-dependencies-are-the-authors-choice.md) removed dependency policing from command handlers. The #3620 follow-up preserves that freedom with narrowly scoped advice for direct decision-reader and token parameters. Reading state as model parameters keeps ordinary decisions independent of acquisition, but not every key can currently be expressed that way.

## Decision

The accepted convention: a command's `Handle()` may take **any service dependencies**. It reads state by declaring the read models it needs as parameters. Direct `IDecisionReads` or `DecisionRead<T>` parameters are almost never needed for the supported shapes.

ARCCHR0040 gives **Info-only** guidance for those two types directly in `Handle()`, naming the alternative: declare the read model as a `Handle` parameter, guarded only when read protection is `On`. `IDecisionReads` acquires state; `DecisionRead<T>` is an issued version-token wrapper, not a reader service. Neither type is removed or forbidden. For a key or stream id that supported parameter shapes cannot express, take IDecisionReads (or DecisionRead<T>) directly in Handle and suppress ARCCHR0040 with a reason and a link to the tracking feature beside the parameter. Do not move readers into `Provide()` merely to silence the diagnostic.

This supersedes only 0015's prohibition on parameter-type diagnostics and its statement that documentation does not discourage these two forms. Arbitrary services remain unrestricted. This is a partial supersession of 0015, as 0015 partially supersedes 0014. ARCCHR0039 stays retired and is never reused. The future #3587 analyzer must receive its own new id, not ARCCHR0040; this record allocates no id for it. There is no warning/error escalation and no code fix. Runtime protection, ARCCHR0011 and ARCCHR0012 are unchanged; replacing a token with a plain model without enabling protection is not a safe automatic rewrite.

## Options considered

- **Narrow Info guidance (chosen).** Explain the ordinary model-parameter alternative without forbidding service dependencies, retaining suppression for real gaps.
- **Keep 0015 unchanged.** Avoid all parameter-type advice, but leave the ordinary state-input convention less discoverable.
- **Restrict dependencies or escalate to warning/error.** Rejected: services are the author's choice and acquisition gaps remain.

## Default if unanswered

0015 remains accepted. Its no-diagnostic policy continues to apply in the absence of this decision; this partial successor does not mark all of 0015 superseded.

## Timeline and scope

Accepted under the project owner's delegation for #3620. Scope is baseline specifications, existing-shape documentation and ARCCHR0040, with no new public runtime API or acquisition shape. Consumer migrations and provider changes are out of scope.

Caller-derived keys, conditional keys, keyed collections, keys derived from other state, and cross-route reads remain open, pending a separate API decision. This record proposes no API spellings for them.

## Verification

**Done when:** existing model-parameter shapes have behavioral specifications, ARCCHR0040 reports only direct reader/token Handle parameters on recognized commands at Info severity, and documentation explains protection and suppression for gaps.

**Verify by:** Chronicle.Specs read-model specifications, Chronicle.CodeAnalysis.Specs positive and exclusion cases, the Release build and documentation checks.

## Consequences

Authors retain every dependency choice. Ordinary state inputs become easier to discover without pretending that unsupported acquisition shapes already work. The project owner accepted this partial successor by delegation.
