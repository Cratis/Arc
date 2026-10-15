---
title: "ARCCHR0038: Stream read guard widens"
description: Combining a per-stream read with another same-source read or a broader declared concurrency policy can cause extra conflicts.
---

Use this diagnostic when another month's activity unexpectedly conflicts. The guard is safe, but it is not as narrow as the per-stream model alone.

## Rule

**Info**, in `Measure` or `On`. A per-stream model parameter accompanies a source-visible whole-source read on the command source, a model of another stream, or effective declared `Concurrency` containing `EventSourceId` without both `EventStreamType` and `EventStreamId`. Validator constructor parameters participate too.

## Resolve the diagnostic

| You need | Do this |
| --- | --- |
| Only this month's folded state | Use only the per-stream model |
| The whole account balance as well | Keep both reads and accept the whole-source guard |
| Exact model event types within this stream | Do not declare an additional broader `Concurrency` policy |
| Any event in this stream to conflict | Declare `EventSourceId \| EventStreamType \| EventStreamId` on the stream |

Widening can add conflicts; it does not miss a change affecting a read. Under `On`, Chronicle retains each dimension the reads agree on: two months of the same stream type lose the id filter, while a whole-source read drops both stream filters. Measure reports read/read widening as `coarsened` with reason `stream_dims_dropped`. See [the monthly limit](../consistency/scenarios/per-stream.md#keep-the-route-exact) for the interaction with source-level fallback policies.

## Boundaries

This is a signature/declaration diagnostic, not dataflow analysis. Advisory reads, different known lookup keys and `Off` are excluded. Arbitrary `IDecisionReads` calls in helpers and dynamic providers are not inferred. Runtime guard merging remains authoritative; absence of this diagnostic does not prove a narrow guard. There is no code fix.
