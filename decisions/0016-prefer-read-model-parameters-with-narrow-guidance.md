---
id: 0016-prefer-read-model-parameters-with-narrow-guidance
title: Prefer read-model parameters with narrow informational guidance
status: proposed
stage: none
decided: null
decider: null
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

[0015](0015-handle-dependencies-are-the-authors-choice.md) removed dependency policing from command handlers. The #3620 follow-up preserves that freedom while proposing narrowly scoped advice for direct decision-reader and token parameters. Reading state as model parameters keeps ordinary decisions independent of acquisition, but not every key can currently be expressed that way.

## Decision

Proposed convention: a command's `Handle()` may take **any service dependencies**. It reads state by declaring the read models it needs as parameters. Direct `IDecisionReads` or `DecisionRead<T>` parameters are almost never needed for the supported shapes.

ARCCHR0040 gives **Info-only** guidance for those two types directly in `Handle()`, naming the alternative: declare the read model as a `Handle` parameter. `IDecisionReads` acquires state; `DecisionRead<T>` is an issued version-token wrapper, not a reader service. Neither type is removed or forbidden. A real acquisition gap warrants suppressing ARCCHR0040 with a reason beside the parameter. Do not move readers into `Provide()` merely to silence the diagnostic.

This supersedes only 0015's prohibition on parameter-type diagnostics and its statement that documentation does not discourage these two forms. Arbitrary services remain unrestricted. ARCCHR0039 stays retired and is never reused. There is no warning/error escalation and no code fix. Runtime protection, ARCCHR0011 and ARCCHR0012 are unchanged; replacing a token with a plain model without enabling protection is not a safe automatic rewrite.

## Options considered

- **Narrow Info guidance (proposed).** Explain the ordinary model-parameter alternative without forbidding service dependencies, retaining suppression for real gaps.
- **Keep 0015 unchanged.** Avoid all parameter-type advice, but leave the ordinary state-input convention less discoverable.
- **Restrict dependencies or escalate to warning/error.** Rejected: services are the author's choice and acquisition gaps remain.

## Default if unanswered

0015 remains accepted. Its no-diagnostic policy continues to apply until this proposed partial successor is accepted; this record does not mark 0015 superseded.

## Timeline and scope

Review with #3620 before release. Scope is baseline specifications, existing-shape documentation and ARCCHR0040, with no new public runtime API or acquisition shape. Consumer migrations and provider changes are out of scope.

Caller-derived keys, conditional keys, keyed collections, keys derived from other state, and cross-route reads remain open, pending a separate API decision. This record proposes no API spellings for them.

## Verification

**Done when:** existing model-parameter shapes have behavioral specifications, ARCCHR0040 reports only direct reader/token Handle parameters on recognized commands at Info severity, and documentation explains protection and suppression for gaps.

**Verify by:** Chronicle.Specs read-model specifications, Chronicle.CodeAnalysis.Specs positive and exclusion cases, the Release build and documentation checks.

## Consequences

Authors retain every dependency choice. Ordinary state inputs become easier to discover without pretending that unsupported acquisition shapes already work. Acceptance of this proposed partial successor remains a human decision.
