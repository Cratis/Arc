---
title: "ARCCHR0037: Read model stream is not on the command route"
description: A per-stream model parameter requires the command's own source, stream and explicit stream id in every mode.
---

Use this diagnostic when a per-stream model cannot select the command's stream. Fix the route rather than falling back to a whole-source read.

## Rule

**Error**, in `Off`, `Measure` and `On`. A `Handle()` or `Provide()` parameter, or a `CommandValidator<T>` constructor parameter, takes a model declaring Chronicle's `[EventStream<TSource>(stream)]`, but the command's source/stream differs or it declares no explicit stream id. `[LookUpBy]` on the parameter also reports this rule: a stream model belongs to the command's own event source.

## Resolve the diagnostic

| Problem | Resolution |
| --- | --- |
| No route, a different definition or stream, or `All` | Route the command with `[EventSource<TSource>(stream)]` matching the model |
| No stream id, or the default id | Add `[EventStreamId("{Month}")]`, another explicit id, or `ICanProvideEventStreamId` |
| A lookup selects another source | Remove `[LookUpBy]`. Use the `IDecisionReads` stream overload in `Provide()` for another source or month |

Matching source/stream string attributes are accepted by value; a typed definition keeps writers aligned. The [monthly-limit example](../consistency/scenarios/per-stream.md) shows the complete route and validator. Runtime mismatches produce `ReadModelStreamNotOnCommandRoute` before reading. Missing template values instead return `400` naming the property.

## Boundaries

The rule checks declarations, not the value returned by a provider or the template's input. Runtime route validation remains authoritative. Keyed services and handler values potentially supplied by `Provide()` are excluded. There is no code fix.
