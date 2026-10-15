---
title: "ARCCHR0036: Exact scope coverage of a protected read is not proven"
description: Exact scopes must cover the same-source reads they accompany.
---

## Rule

**Warning**, in Measure or On. A command returns exact concurrency scopes
(including a stream decision's append result) beside an automatically resolved
protected read on the command source. Coverage is not proven by that signature.

## Resolve the diagnostic

Widen the exact scope to cover the read's source, event types, dimensions and
boundary, or remove the exact scope and let protected reads supply their guards.
When an explicit scope intentionally covers every read, suppress with a
justification after checking coverage.

A per-stream model in a validator is read before `Provide()`. A stream decision acquired there can have a later boundary and fail with `ExplicitScopeDoesNotCoverReads`; that validator-first signature is reported too. Prefer the per-stream model alone for folded state, or the stream decision alone for raw facts. A direct `Provide(IStreamReads reads) => reads.ForCommand()` with a per-stream `Handle()` parameter on that same route is not reported for this combination.

## Boundaries

Advisory reads, different statically known keys, custom source providers and
Handle values potentially supplied by Provide are excluded. This heuristic does
not prove predicates or boundaries; runtime scope validation is authoritative.
No code fix is offered. ARCCHR0027 belongs to refusal-branch validation, not this rule.
