---
title: Fresh identifiers
description: Reject a generated append target that already has events, without rerunning the command handler.
---

A generated identifier is meant to open a new event source, not accidentally add facts to an existing one.
With Arc's Chronicle integration, a [`[GeneratedValue]` Handle parameter](../../commands/generated-values.md)
uses `GeneratedValueFreshness.Guarded` by default. When an Arc-owned append or a write through the command's
unit of work targets that value, Arc requires
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
- Direct `IUnitOfWork.AddEvent`, `AddEvents` and `AddEventWithNamedTags` calls on the command's unit of work.
- `eventLog.Transactional.Append`, `AppendMany`, `AppendWithNamedTags` and `AppendManyWithNamedTags` on that unit.

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
Routing metadata, concurrency attributes and the normal concurrency strategy cannot replace it with a later
tail or a narrowed expectation. Exact scopes supplied through `EventsWithConcurrencyScopes` or direct
unit-of-work and transactional calls are checked for conflicts. A compatible explicit whole-source `BeforeFirst` scope merges with the guard,
including scopes emitted by Stage. A different explicit intent, such as a narrowed scope, a tail revision
or `ConcurrencyScope.None`, fails the command with `ConflictingConcurrencyScopes` before the batch is
enrolled or appended.

Without a transaction, Arc keeps each generated source's events in one atomic append. If unrelated legacy
routes require individual appends, the generated source's events must be contiguous in the returned
collection. Interleaving them with another source would require splitting the freshness guard or changing
event order, so Arc rejects that shape with `GeneratedIdentifierEventsCannotBeSplit` before any append.
Return the generated source's events together, use `EventsWithConcurrencyScopes`, or use a command
transaction.

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
not acquire that detail. Arc's collision and `ConflictingConcurrencyScopes` messages do not include the
generated value. Arc also maps conflicts Chronicle detects against an already enrolled generated guard to
`ConflictingConcurrencyScopes`, using typed scope properties and the command unit's provenance rather than
parsing messages. Unrelated enrollment conflicts retain Chronicle's own redacted failure; they are not
classified as generated-identifier conflicts.

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

## Direct writes through the command's unit of work

The direct and transactional APIs listed above receive the same whole-source guard and collision
classification as returned events. This applies only to the command's actual unit of work, not arbitrary
new units or external stores. Returned, aggregate and direct events for the same generated source share
one expectation; an occupied source rejects the whole pending unit, including events for other sources.

Nested pipeline invocations share the outer command's unit. When Arc composes returned-event or aggregate
scopes, it considers guarded captures from every active invocation on that unit. A nested handler's
attribute-derived scope therefore cannot narrow an outer generated identifier's whole-source guard.
Truly caller-supplied scopes remain subject to the explicit conflict check.

After successful enrollment, Chronicle retains the required scope even when that invocation's contributor
is disposed. A later unscoped write inherits the requirement. If Chronicle rejects an incompatible supplied
scope before Arc's contributor runs, Arc translates conflicts against that recorded generated guard to
`ConflictingConcurrencyScopes`. Scope-only `AddEvents` entries preserve existing
requirements but do not call contributors, create a new freshness guard or record generated provenance.
An identifier captured by a completed nested invocation but never targeted has no enrolled requirement.

Nested pipeline scopes leave commit and rollback to the outer command. Propagate a nested rejection so the
outer command rolls back instead of committing earlier events. An explicit aggregate `Commit()` can still
finalize the shared unit early; see [Nested commands and aggregates](transactional-commands.md#nested-commands-and-aggregates).

Custom `IUnitOfWork` implementations must support Chronicle's enrollment contributors. A handler declaring
at least one guarded `[GeneratedValue]` parameter fails closed with Arc's
`FreshIdentifierGuardRequiresEnrollmentContributors` if its unit cannot register one. Commands without generated
parameters, or with only `Unguarded` parameters, do not require this support.

`IUnitOfWork.AddEvents` resolves scope requirements for all destinations before staging events in that
call. A scope conflict stages none of that call's events; a contributor notification failure occurs after
staging and prevents the unit from committing.
`Transactional.AppendMany` and `AppendManyWithNamedTags` stage per event instead: a later rejection may
leave earlier events pending. Let the exception escape `Handle()` so Arc rolls back the whole unit.
Catching the rejection and succeeding can commit earlier enrollments.

### Immediate appends remain outside the guard

Non-transactional `eventLog.Append`, `AppendMany` and their named-tag variants inside `Handle()` bypass
unit-of-work enrollment. They remain immediate, cannot be rolled back by the command and do **not** receive
automatic freshness protection. Prefer returned events or `eventLog.Transactional` on the command's unit. Under read protection `On`, an immediate append is refused with `ImmediateAppendWhileReadsAreProtected`.
An immediate caller can supply an explicit whole-source `BeforeFirst` scope and inspect the append result,
but that write has no automatic generated-target provenance and need not report `GeneratedIdentifierCollision`.

## Verify an occupied identifier in a spec

Create a [command scenario](../../testing/generated-values.md) with `UseDecisionReads()` **before seeding**,
then pin the generated parameter by name. In this mode, `Given.ForEventSource(id).Events(...)` seeds the same
event log the freshness guard checks. Default mode seeds read-model state only and leaves that log empty;
it cannot prove that a generated identifier is occupied.

This excerpt uses `CreateSource`, `Identifier`, `Created` and `ResultShape` from the
[fresh-identifier spec context](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Specs/Commands/for_CommandScenario_with_fresh_identifiers/given/a_fresh_identifier_scenario.cs).
Import `Cratis.Arc.Testing.Commands`, `Cratis.Arc.Chronicle.Testing.Commands`, `Cratis.Arc.Validation`
and `Cratis.Specifications`:

```csharp
await using var scenario = new CommandScenario<CreateSource>().UseDecisionReads();
var id = new Identifier(Guid.Parse("fb6dd43a-b161-44b1-9baa-7b2e4a071680"));
scenario.PinGeneratedValue("id", id);
scenario.Given.ForEventSource(id).Events(new Created("existing"));
var tailBefore = await scenario.EventLog.GetTailSequenceNumber();
var command = new CreateSource(ResultShape.Wrapper);

var result = await scenario.Execute(command);

result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
result.ValidationResults.Single().ReasonDetail.ShouldEqual("GeneratedIdentifierCollision");
(await scenario.EventLog.GetTailSequenceNumber()).ShouldEqual(tailBefore);
command.Calls.ShouldEqual(1);
```

Default mode now throws `GeneratedIdentifierCollisionRequiresDecisionReads` before handling the command:
"The scenario cannot prove a generated-identifier collision without UseDecisionReads(). Create the scenario
with `new CommandScenario<TCommand>().UseDecisionReads()` before seeding events and pinning the generated value."

Seed an event for the same identifier, including a different stream if your command routes to a specific
stream. Also exercise an empty source with two events through your chosen append API: both must append successfully under one guard.

A fixed pin is reused across executions. A second create invocation with the same pin should collide rather
than silently continue the first source.

## Supported Chronicle versions

Direct-write freshness and redacted scope-conflict translation require Chronicle client **19.65.2 or later**. Deploy a Chronicle kernel **19.40.1 or later**
for retry-time revalidation of expected-empty scopes; upgrading a client alone does not upgrade the kernel.
Chronicle 19.59.2 and later also normalize equal `BeforeFirst` batch scopes. Use a current compatible kernel
when several destinations or explicit scope labels share one batch.

For stateful decisions over an existing source, see [Concurrency](concurrency.md) and
[choosing how a command's rule holds](../consistency/index.md).
