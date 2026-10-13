---
id: 0010-declarative-reactor-command-refusals
title: Handle reactor command refusals with ordered per-invocation branches
status: accepted
stage: implemented
decided: 2026-10-13
decider: woksin (via L0)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Arc.Core/Commands/**
  - Source/DotNET/Chronicle/Reactors/**
  - Documentation/backend/csharp/chronicle/reactors/command-side-effects.md
---

<!-- Copyright (c) Cratis. All rights reserved. -->
<!-- Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

A failed command returned from a reactor currently fails the observer partition. Applications can inspect the pipeline imperatively, but neither generated reactors nor a static model can express ordered business-refusal branches. [Arc #3058](https://github.com/Cratis/Arc/issues/3058) supplies the runtime contract needed by [Screenplay decision 0030](https://github.com/Cratis/Screenplay/blob/main/decisions/0030-reaction-refusals-and-redelivery.md), without changing Chronicle's failure and recovery semantics. Decisions 0005 and 0008 remain in force: declared blocking severity and pipeline authorization boundaries do not change.

## Decision

Classify command business refusals in Arc.Core as validation, constraint with an optional name, or authorization, and expose ordered per-invocation branches through `Invoke.Command(command).OnRefused(selector, handler)`. Error-severity authored rules and constraints qualify; actual unauthorized results qualify, not Unsupported outcomes. Exceptions and error-severity concurrency, infrastructure, contract and unknown validation reasons prevent classification, including mixed results; warning and information results never prevent classification. Among business refusals authorization wins over constraint, which wins over validation. Constraint refusals carry all distinct violated names in `ConstraintNames`, with `ConstraintName` retaining the first error constraint's name; named selectors match any violated name. All error constraint messages join in result order with a newline; validation joins error rule messages in result order; authorization preserves its reason. Server-only authorization configuration failure markers and framework-composed stream-completion errors prevent refusal classification without changing HTTP authorization responses. Text is not trimmed or translated. `Refused` is an inspectable sealed record whose matched kinds and optional constraint name determine matching and value equality. `Refused.ByAnyConstraint` selects any constraint, while `Refused.ByConstraint(name)` requires a name. The first matching branch returns registered events or null, and successful handling stops remaining commands. `.Acknowledge(Refused.X)` is the single acknowledgment convenience and maps to Screenplay `acknowledge`; there is no `Invoke.Acknowledge`. `Refused.Any` matches validation and constraints, never authorization. Branch productions use the ordinary returned-event append path and routing, and an append failure fails the partition. Plain command behavior and mixed results' events-first partitioning remain unchanged. No reflection is added for branch selection or execution.

## Options considered

- **Per-invocation declarations (chosen).** Unambiguous with repeated command types, readable by generators, and compatible with existing result shapes.
- **Reactor method naming conventions such as OnReserveRefused (rejected).** Cannot identify which of several invocations owns the branch and hides declaration order.
- **Imperative ICommandPipeline result inspection (not chosen for this contract).** Still supported, but not statically modelable as invocation-local branches.
- **Structured Chronicle append failures (rejected here).** They still fail the partition and cannot express successful acknowledgment at the Arc command boundary.
- **Treat concurrency or unknown outcomes as refusals (rejected).** Could acknowledge an uncompleted effect or a broken contract instead of retrying or failing closed.

## Default if unanswered

Without this contract, plain command failures keep failing the partition and generated refusal handling remains unavailable. woksin accepts the additive contract with the inspectable selector, single acknowledgment convenience, error-only non-refusal filtering, explicit any-constraint selector and explicit exclusion of Unsupported authorization outcomes recorded above.

## Timeline and scope

Applies to Arc's additive refusal-handling contract and subsequent generator extraction. In scope: classification, ordered selectors, acknowledgment, branch event append and stop-after-handled semantics. Out of scope: Screenplay admission, Stage rendering, generator extraction and analyzer coverage for refusal branches returning non-events (Arc #3094), redelivery declarations, new actor semantics, cross-command transactions and external-effect deduplication. A blocking warning under decision 0005 is not an error-severity refusal and retains failure/recovery behavior. Existing system-execution roles and decision 0008's trust boundaries remain unchanged.

## Verification

- **Done when:** classifier specifications distinguish every business kind from non-refusal failures; first-match and explicit authorization behavior hold; handled refusals stop remaining invocations; branch events preserve routing; branch append failures surface; and plain commands and mixed events-first results retain their behavior.
- **Verify by:** run Arc.Core.Specs, Chronicle.Specs and Chronicle.CodeAnalysis.Specs, the whole-solution Release build, AOT ratchet, changed-file whitespace checks and documentation authoring/link gates, followed by independent review of the whole change.

## Consequences

Reactors can record or acknowledge an expected business refusal without hiding retryable failures. Prior accepted effects remain committed and may repeat on recovery. Authored messages are available verbatim, but callers must match constraint identity rather than parsing prose. Custom command side-effect executors must implement the new context-and-event-store overload to support branch productions. Screenplay and Stage retain their own admission and rendering responsibilities.
