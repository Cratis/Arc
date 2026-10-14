---
title: "ARCCHR0023: Refusal branch must return registered events or acknowledge"
description: Correct statically invalid OnRefused callback results before they fail a reactor delivery.
---

## Rule

A `CommandInvocation.OnRefused` callback produces registered events, not further command work. **Default severity: Warning.**

The analyzer reports statically visible invalid returns in expression or block lambdas, anonymous methods, and method groups whose source bodies are available. It checks conditional and switch-expression arms and literal collections. Commands, operations, stream completions, non-event objects, empty collections, null batch members, and envelopes containing invalid payloads are rejected.

The runtime validates the complete refusal production before appending any branch events. One invalid member fails the delivery without appending the valid members of that branch. Earlier successful commands and previously appended events are not rolled back.

## Fix

Return an event marked `[EventType]`, an `EventForEventSourceId` containing one event, or a stable nonempty array or list of those. Register the event types as usual. Return `null` or use `.Acknowledge(selector)` when handling the refusal should produce no events. An envelope must not contain another envelope or an event collection.

Do not return a command from the refusal callback to try another operation. Choose the subsequent work outside the branch; a handled refusal stops the delivery's remaining command invocations. There is no automatic code fix because the intended refusal outcome is a domain decision.

## Boundaries

Only the actual Arc `CommandInvocation.OnRefused` method is checked; similarly named APIs are ignored. Method-group bodies outside the current compilation remain opaque, even when a project reference exposes their syntax. Collection-builder factory results are also opaque: an empty input does not prove that a custom builder returns an empty batch. The analyzer does not follow helper calls, erased object values, unknown generic elements, custom enumerators, or mutable collection dataflow. Nested functions are not callback returns unless supplied directly as the callback. A quiet analyzer is not proof of event registration, collection stability, or a valid runtime result.

See [handling a command refusal](../reactors/command-side-effects.md#handle-a-command-refusal) for branch ordering, selectors, and failure behavior.
