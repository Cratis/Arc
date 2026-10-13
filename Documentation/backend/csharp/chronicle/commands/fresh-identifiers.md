---
title: Fresh identifiers
description: Reject a generated append target that already has events, without rerunning the command handler.
---

A generated identifier is meant to open a new event source, not accidentally add facts to an existing one.
With Arc's Chronicle integration, a [`[GeneratedValue]` Handle parameter](../../commands/generated-values.md)
uses `GeneratedValueFreshness.Guarded` by default. When an Arc-owned append targets that value, Arc requires
the **whole event source** to be empty before the command's events are appended.

## What is guarded

Arc compares each actual append destination with the validated values generated in the same invocation.
It unwraps concepts and typed event-source identifiers. A Guid uses lowercase `D` format; strings compare
exactly, with ordinal case sensitivity. A string that looks like a Guid remains a string. Other supported
primitive identities use Chronicle's invariant formatting, including round-trip dates and times.
Arbitrary records' display strings are not identifiers.

The guard covers:

- Returned events and event collections, including an identifier and events returned together in a tuple.
- Single or multiple `EventForEventSourceId` wrappers, including collections mixed with plain events.
- `EventsWithConcurrencyScopes`, preserving compatible explicit whole-source emptiness scopes.
- Aggregates acquired with `IAggregateRootFactory.Get` using a guarded generated identifier, and their mutations.

Values used only in a payload or response add no scope. For example, a generated API key returned to the
caller does not guard the customer's event source. If several generated parameters have the same value,
one guarded capture is enough to guard that destination.

This is a Chronicle integration policy. Arc.Core captures the policy but does not depend on Chronicle.

## Whole-source emptiness, not an append-time tail

The guard is equivalent to:

```csharp
new ConcurrencyScope(EventSequenceNumber.BeforeFirst, EventSourceId: id)
```

This expression uses `Cratis.Chronicle.Events` and `Cratis.Chronicle.EventSequences.Concurrency`; `id` is the
actual append destination. It has no stream, source-type or event-type narrowing. Existing events in a
different stream or of another type still occupy that identifier.

Several events for one new source share the same immutable expectation: all can succeed in one transaction.
Routing metadata and the normal concurrency strategy cannot replace it with a later tail. A compatible
explicit whole-source `BeforeFirst` scope merges with it, including scopes emitted by Stage. A different
explicit intent, such as a narrowed scope, a tail revision or `ConcurrencyScope.None`, fails the command
with `ConflictingConcurrencyScopes` before the batch is enrolled or appended.

An aggregate factory checks whole-source emptiness before constructing, rehydrating or activating the
aggregate. That read is an early rejection, not the atomic guarantee: mutation still enrolls the whole-source
`BeforeFirst` scope, so an intervening writer is rejected when the unit commits.

## Handle a collision

An occupied generated destination produces a validation result with:

| Field | Value |
| --- | --- |
| `Reason` | `ValidationResultReason.ConcurrencyViolation` |
| `ReasonDetail` | `"GeneratedIdentifierCollision"` |

Arc classifies this result using the generated guard's provenance. An unrelated `BeforeFirst` failure does
not acquire that detail. Collision and conflicting-scope messages do not include the generated value.

Arc never reruns `Handle()`. To retry a collision, submit a **new command invocation**, which generates a new
value. Do not keep retrying a fixed scenario pin against an occupied source. This policy is not an
idempotency protocol: after an uncertain commit, determine whether the first invocation committed before
creating another source.

## Opt out deliberately

`GeneratedValueAttribute.Freshness` is settable. `GeneratedValueFreshness` is in `Cratis.Arc.Commands`.
For an existing generated parameter, the explicit opt-out is:

```csharp
[GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)]
```

Import `Cratis.Arc.Commands.ModelBound` for the attribute and `Cratis.Arc.Commands` for the enum.
`Unguarded` keeps generation, validation and capture, but leaves concurrency protection to your command's
own policy. Use it only when appending to an existing source is intentional; it is not a collision-retry
strategy. The capture exposes the immutable policy through `GeneratedValue.Freshness`.

Use high-entropy identifier strategies such as `GenerateValue.Uuid()` or `GenerateValue.UuidV7()`. Arc does
not infer entropy from a concept's name or enforce an entropy threshold. Client-minted and natural
identifiers are not generated Handle captures: protect their uniqueness with a Chronicle
[constraint](/chronicle/constraints/), rather than assuming this guard covers them.

## Direct unit-of-work writes are not guarded

User code calling `IUnitOfWork.AddEvent`, `IUnitOfWork.AddEvents` or `eventLog.Transactional.Append` directly
inside `Handle()` does **not** receive this automatic freshness guard. Return events through Arc's response
handlers instead, or supply an explicit whole-source `BeforeFirst` scope yourself. Returning
[`EventsWithConcurrencyScopes`](events.md#events-with-exact-concurrency-scopes) is the supported workaround
when the handler must compose explicit intents.

An explicitly supplied scope on an otherwise unguarded direct write can reject occupancy, but it does not
have Arc's generated-target provenance and therefore need not report `GeneratedIdentifierCollision`.

## Verify an occupied identifier in a spec

Use a [command scenario](../../testing/generated-values.md) and pin the generated parameter by name before
execution. Seed an event for that same identifier, including a different stream if your command routes to a
specific stream. Execute once and assert `ConcurrencyViolation`, `ReasonDetail == "GeneratedIdentifierCollision"`,
no additional persisted events, and one handler invocation. Also exercise an empty source with two returned
events: both must append successfully under one guard.

A fixed pin is reused across executions. A second create invocation with the same pin should collide rather
than silently continue the first source.

## Supported Chronicle versions

This Arc integration requires Chronicle client **19.59.4**. Deploy a Chronicle kernel **19.40.1 or later**
for retry-time revalidation of expected-empty scopes; upgrading a client alone does not upgrade the kernel.
Chronicle 19.59.2 and later also normalize equal `BeforeFirst` batch scopes. Use a current compatible kernel
when several destinations or explicit scope labels share one batch.

For stateful decisions over an existing source, see [Concurrency](concurrency.md) and
[choosing how a command's rule holds](../consistency/index.md).
