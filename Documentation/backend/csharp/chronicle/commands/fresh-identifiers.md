---
title: Guard generated Chronicle event source ids
description: Keep generated event source ids from extending an existing Chronicle history, handle collisions, and verify that rejected commands append nothing.
---

Cratis Arc guards generated event source ids so a create command cannot accidentally extend an existing history in Cratis Chronicle. A [`[GeneratedValue]` Handle parameter](../../commands/generated-values.md) uses `GeneratedValueFreshness.Guarded` by default. When returned events or writes through the command's unit of work target that value, the whole event source must be empty.

An occupied destination rejects the command; Arc does not rerun `Handle()`. Keep the rejection visible to the caller and submit a new invocation only when creating another identity is the intended recovery.

## What is guarded

The guard follows the append destination, not every use of the generated value. Arc compares that destination with the invocation's validated generated values after unwrapping concepts and typed event-source identifiers.

A Guid uses lowercase `D` format. Strings compare exactly with ordinal case sensitivity, even when they look like Guids. Other supported primitive identities use Chronicle's invariant formatting, including round-trip dates and times. Arc does not treat an arbitrary record's display string as an identifier.

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

Several events for one new source share the same expectation and can succeed in one transaction. Routing metadata, concurrency attributes and the normal strategy cannot replace whole-source emptiness with a later tail or a narrower check.

Arc checks explicit scopes from `EventsWithConcurrencyScopes`, direct unit-of-work calls and transactional calls for conflicts. A whole-source `BeforeFirst` scope merges with the guard, including scopes emitted by Stage. A narrowed scope, a tail revision or `ConcurrencyScope.None` expresses a different requirement and fails with `ConflictingConcurrencyScopes` before the batch is enrolled or appended.

Without a transaction, Arc appends a returned collection as one batch, so a generated source's freshness guard
stays in the same atomic append as every other event of the command, whatever routes those events use and
wherever the generated source's events sit in the collection.

An aggregate factory checks whole-source emptiness before constructing, rehydrating or activating the
aggregate. That read is an early rejection, not the atomic guarantee: mutation still enrolls the whole-source
`BeforeFirst` scope, so an intervening writer is rejected when the unit commits.

## Handle a collision

An occupied generated destination produces a validation result with:

| Field | Value |
| --- | --- |
| `Reason` | `ValidationResultReason.ConcurrencyViolation` |
| `ReasonDetail` | `"GeneratedIdentifierCollision"` |

Arc records which scope came from a generated-value guard and uses that origin to classify collisions. An unrelated `BeforeFirst` failure does not get `GeneratedIdentifierCollision`. Neither the collision message nor Arc's `ConflictingConcurrencyScopes` message includes the generated value.

If Chronicle detects a conflict with a generated guard already enrolled in the command's unit, Arc maps it to `ConflictingConcurrencyScopes`. The mapping uses typed scope properties and the recorded guard origin, not message parsing. Unrelated enrollment conflicts retain Chronicle's own redacted failure.

To retry a collision, submit a new command invocation, which generates a new value. Repeating a fixed scenario pin against an occupied source will keep failing. After an uncertain commit, first determine whether the original invocation committed: this guard prevents identity reuse, not duplicate creation under different ids.

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

The outer command owns commit and rollback for nested pipeline invocations. Propagate a nested rejection; treating it as success can commit events enrolled earlier in the outer command. An explicit aggregate `Commit()` can still finalize the shared unit early. See [Nested commands and aggregates](transactional-commands.md#nested-commands-and-aggregates) before mixing these paths.

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

Create a [command scenario](../../testing/generated-values.md), then pin the generated parameter by name.
Guarded generated parameters automatically select the in-process log before seeding.
`Given.ForEventSource(id).Events(...)` seeds the same event log the freshness guard checks.

This excerpt uses `CreateSource`, `Identifier`, `Created` and `ResultShape` from the
[fresh-identifier spec context](https://github.com/Cratis/Arc/blob/main/Source/DotNET/Chronicle.Specs/Commands/for_CommandScenario_with_fresh_identifiers/given/a_fresh_identifier_scenario.cs).
Import `Cratis.Arc.Testing.Commands`, `Cratis.Arc.Chronicle.Testing.Commands`, `Cratis.Arc.Validation`
and `Cratis.Specifications`:

```csharp
await using var scenario = new CommandScenario<CreateSource>();
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

The assertions check the failure category, an unchanged log tail and a single call to `Handle()`. Seed the same identifier in a different stream as well if your command has a specific route; that history must still cause a collision. Also test an empty source with two events through your chosen append API. Both events should append under the shared guard.

A fixed pin is reused across executions. A second create invocation with the same pin should collide rather
than silently continue the first source.

## Supported Chronicle versions

Direct-write freshness and redacted scope-conflict translation require Chronicle client **19.65.2 or later**. Deploy a Chronicle kernel **19.40.1 or later**
for retry-time revalidation of expected-empty scopes; upgrading a client alone does not upgrade the kernel.
Chronicle 19.59.2 and later also normalize equal `BeforeFirst` batch scopes. Use a current compatible kernel
when several destinations or explicit scope labels share one batch.

For stateful decisions over an existing source, see [Concurrency](concurrency.md) and
[choosing how a command's rule holds](../consistency/index.md).
