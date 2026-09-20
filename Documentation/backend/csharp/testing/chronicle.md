---
uid: Arc.Testing.ChronicleExtension
title: Chronicle testing (optional)
description: Extend Arc command scenarios with an in-process Chronicle event log and explicitly seeded read model state.
---

This page applies **only to commands using the optional Chronicle integration**. Standalone Arc commands use [Cratis.Arc.Testing](./command-scenario.md) and do not need an event store or this extension.

When `Cratis.Arc.Chronicle.Testing` (or the `Cratis.Testing` meta-package) is referenced, `CommandScenario<TCommand>` is automatically extended with an in-memory event scenario. No separate class or base type is needed.

The extension is wired via `ChronicleCommandScenarioExtender`, which implements `ICommandScenarioExtender` and is discovered automatically by `CommandScenario<TCommand>` at construction time using the Cratis type discovery system.

In a Cratis Specification, that gives you the event-sourced test shape directly: seed prior facts with `_scenario.EventScenario.Given` in `Establish()`, execute the command once in `Because()`, then assert the `CommandResult` and the captured events from `[Fact]` methods.

## Package

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Arc.Chronicle.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Specifications.XUnit" />
<PackageReference Include="Cratis.Testing" />
```

## Basic usage

The C# examples below are **illustrative spec fragments**, not standalone runnable checkpoints. They assume your application's commands, event types, projections/reducers, and named constraints already exist and are discovered. Seeding an event does not invent a uniqueness rule. Use the [standalone testing checkpoint](./index.md) to learn the scenario lifecycle first.

Import `Cratis.Arc.Testing.Commands` for Arc assertions, `Cratis.Arc.Chronicle.Testing.Commands` for the C# 14 extension properties and capture assertions, `Cratis.Chronicle.Testing.EventSequences` for event-log assertions, and your normal `Cratis.Specifications` / `Xunit` namespaces. Event IDs and sequence numbers live in `Cratis.Chronicle.Events` and `Cratis.Chronicle.EventSequences`. Dispose each scenario in `Destroy()` as shown in [command scenarios](./command-scenario.md#disposal).

Use the same `CommandScenario<TCommand>` class as for non-Chronicle commands. When the Chronicle testing package is present, these extension properties are available directly on the scenario:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `Given` | `CommandScenarioChronicleGivenBuilder<TCommand>` | Seed the read model state a command observes — `Given.ForEventSource(id).Events(...)` or `.ReadModel(...)` |
| `EventScenario` | `EventScenario` | The full event scenario — use `EventScenario.Given` to seed the event log |
| `EventLog` | `IEventLog` | The in-memory event log — use for appending and assertions |
| `EventSequence` | `IEventSequence` | The event scenario's sequence — use with Chronicle's assertion helpers |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | Appends captured since scenario construction, including event-log seeding |

```csharp
public class when_registering_author : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_authorId, "Jane Austen"));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_appended_registered_event() =>
        _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(_authorId);
}
```

The spec executes the command once and then asserts both the Arc result and the Chronicle fact that was recorded.

## Seeding pre-existing events with `Given`

Use `_scenario.EventScenario.Given` to append events to the in-memory event log *before* the command runs. Call `ForEventSource` with the event source identifier, then pass the pre-existing events to `Events`:

```csharp
public class when_registering_author_with_same_name : Specification
{
    readonly EventSourceId _authorId = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    Task Establish() =>
        _scenario.EventScenario.Given
            .ForEventSource(_authorId)
            .Events(new AuthorRegistered("Jane Austen"));

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_authorId, "Jane Austen"));

    [Fact] void should_not_succeed() =>
        _result.ShouldNotBeSuccessful();

    [Fact] void should_report_the_existing_author() =>
        _result.ShouldHaveValidationErrorFor("Author is already registered");

    [Fact] Task should_not_have_appended_a_second_event() =>
        _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

Seed events before calling `Execute` so they are present when the command handler runs. This example assumes the application's validator rejects a repeated registration with the message `"Author is already registered"`. The message assertion pins that intended rejection: a missing dependency alone must not make this spec pass. Event-log seeding alone does not seed injected read models; use the separate `Given` builder below if the validator reads projected state.

## EventLog assertion helpers

Chronicle's `EventSequenceShouldExtensions` extend `IEventSequence`. Call them on `_scenario.EventLog` or `_scenario.EventSequence`. They read the log, including pre-existing events, rather than only the latest command's output.

| Call shape (all return `Task`) | Meaning |
| --- | --- |
| `ShouldHaveAppendedEvent<TEvent>()` | An event of that type exists anywhere in the sequence |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId)` | An event of that type exists for that source |
| `ShouldHaveAppendedEvent<TEvent>(eventSourceId, predicate)` | A matching event for that source satisfies `Func<TEvent, bool>` |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber)` | That exact global sequence position contains the type |
| `ShouldHaveAppendedEvent<TEvent>(sequenceNumber, eventSourceId, predicate)` | That exact position also belongs to the source and satisfies the predicate |
| `ShouldHaveTailSequenceNumber(expected)` | The log's tail equals the expected `EventSequenceNumber` |

Content overloads also accept `Action<TEvent>` assertions. An action validates the first matching event; a predicate searches for a satisfying event. For all overloads, see the [Chronicle event assertions reference](/chronicle/testing/events/assertions/).

## Transactional commands in tests

The harness runs commands with the same [transactional scope](../commands/transactional-commands.md) as production: the events a command returns — and appends through `eventLog.Transactional` — commit atomically when it succeeds and roll back when it fails, including when a unique constraint rejects the commit. Immediate appends through `IEventLog`/`IEventStore.EventLog` land right away and are final, but a failed one fails the command. That gives specs two natural assertions: the intended rejection and the absence of committed events.

The following fragment assumes the application's registered unique-name constraint covers `AuthorRegistered.Name` across authors. `AuthorConstraintNames.UniqueName` is the application's string constant for that constraint's registered name, not an Arc-provided name. Use your actual constraint-name constant; seeding an event alone does not register a constraint.

```csharp
public class when_registering_author_with_taken_name : Specification
{
    readonly EventSourceId _existing = EventSourceId.New();
    readonly EventSourceId _author = EventSourceId.New();
    readonly CommandScenario<RegisterAuthor> _scenario = new();
    CommandResult _result = default!;

    Task Establish() =>
        _scenario.EventScenario.Given
            .ForEventSource(_existing)
            .Events(new AuthorRegistered("Jane Austen"));

    async Task Because() =>
        _result = await _scenario.Execute(new RegisterAuthor(_author, "Jane Austen"));

    [Fact] void should_not_succeed() =>
        _result.ShouldNotBeSuccessful();

    [Fact] void should_be_rejected_by_the_uniqueness_constraint() =>
        _result.ShouldHaveConstraintViolationFor(AuthorConstraintNames.UniqueName);

    [Fact] async Task should_append_nothing_for_the_rejected_author() =>
        (await _scenario.EventLog.HasEventsFor(_author)).ShouldBeFalse();
}
```

The named assertion checks both `ConstraintViolation` and the exact constraint name, so a dependency-resolution failure or a different constraint cannot satisfy it. Keep the no-events assertion as well: it verifies the separate storage contract.

Two things to be aware of:

- **When events show up in `AppendedEvents` depends on the style.** Immediate appends surface as they happen; the command's enrolled events — returned events and `Transactional` appends — surface as one batch when the command's transaction commits.
- **Immediate appends are final.** A handler that appends through the plain `IEventLog.Append` (or `IEventStore.EventLog`) writes immediately — a successful append remains in the log even when the command fails afterwards, and a spec can assert exactly that.

## Testing commands that use EventForEventSourceId

When a command handler returns `EventForEventSourceId` or `IEnumerable<EventForEventSourceId>`, it can target sources other than the command's own source. **Both** event-log assertions and the scenario-level helpers below support filtering by event source ID. Choose log assertions to verify readable stored events; choose scenario helpers to inspect appends captured through the client-side `AppendOperations` observable.

The capture subscription starts when the scenario is constructed, not at each `Execute`. `AppendedEvents` accumulates event-log seeding and subsequent executions. Use a fresh scenario, a source unique to the command, or an explicit baseline when distinguishing new appends from setup. A matching seeded event must not be mistaken for evidence that the command appended it.

| Method | Asserts that... |
| ------ | --------------- |
| `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId)` | At least one event of type `TEvent` was appended for the given `EventSourceId` |
| `ShouldHaveAppendedEvent<TCommand, TEvent>(eventSourceId, predicate)` | Same, and the event also satisfies the predicate |
| `ShouldHaveTailSequenceNumber<TCommand>(expected)` | The highest sequence number among all captured events equals `expected` |

### Example: Single cross-source event

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

public class when_migrating_customer_to_new_id : Specification
{
    readonly CommandScenario<MigrateCustomerToNewId> _scenario = new();
    readonly EventSourceId _oldId = EventSourceId.New();
    readonly EventSourceId _newId = EventSourceId.New();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new MigrateCustomerToNewId(_oldId, _newId));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_appended_migrated_event_for_new_id() =>
        _scenario.ShouldHaveAppendedEvent<MigrateCustomerToNewId, CustomerMigrated>(_newId);

    [Fact] Task should_reference_old_id_in_event() =>
        _scenario.ShouldHaveAppendedEvent<MigrateCustomerToNewId, CustomerMigrated>(
            _newId,
            e => e.OldCustomerId == _oldId);
}
```

### Example: Multiple cross-source events (fund transfer)

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

public class when_transferring_funds : Specification
{
    readonly CommandScenario<TransferFunds> _scenario = new();
    readonly EventSourceId _fromAccount = EventSourceId.New();
    readonly EventSourceId _toAccount = EventSourceId.New();
    CommandResult _result = default!;

    async Task Because() =>
        _result = await _scenario.Execute(new TransferFunds(_fromAccount, _toAccount, 250m));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_debited_from_account() =>
        _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsDebited>(_fromAccount);

    [Fact] Task should_have_credited_to_account() =>
        _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsCredited>(_toAccount);

    [Fact] Task should_have_debited_correct_amount() =>
        _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsDebited>(_fromAccount, e => e.Amount == 250m);

    [Fact] Task should_have_appended_two_events() =>
        _scenario.ShouldHaveTailSequenceNumber<TransferFunds>(1ul);
}
```

> **Sequence numbering applies here too**: `ShouldHaveTailSequenceNumber` checks the highest sequence number across captured appends and fails if none were captured. A tail of `1` means two total events only for a fresh, contiguous sequence starting at `0`. It is not a general count of events from the last command.

The `AppendedEvents` extension property gives you the raw list if you need to write custom assertions:

```csharp
[Fact]
void should_have_exactly_two_events() =>
    _scenario.AppendedEvents.Count.ShouldEqual(2);
```

## Testing commands that take read model dependencies

A command handler, `Provide` method, or `CommandValidator<T>` can take a read model as a parameter — Arc resolves it for the command's event source id exactly as it does at runtime (`IProjectionFor<T>`, `IReducerFor<T>`, and model-bound projections). See [Use current state in a command](/arc/scenarios/use-current-state-in-a-command/) for the production-side pattern. To test such a command you need to control what that read model contains, and the awkward way is to hand-mock `IReadModels`.

`_scenario.Given.ForEventSource(id)` does it for you, two ways: seed the **events** the read model is built from, or pin a materialized **instance** directly.

### Seeding read model state from events

State the events that happened for the event source. Any read model a command injects for that source is materialized from those events through its own reducer or projection — you never name the read model type here, just as you never do in production:

```csharp
public class when_withdrawing_with_sufficient_funds : Specification
{
    readonly EventSourceId _accountId = EventSourceId.New();
    readonly CommandScenario<Withdraw> _scenario = new();
    CommandResult _result = default!;

    void Establish() =>
        _scenario.Given
            .ForEventSource(_accountId)
            .Events(new MoneyDeposited(100m), new MoneyDeposited(50m));

    async Task Because() =>
        _result = await _scenario.Execute(new Withdraw(_accountId, 120m));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();
}
```

Events are the facts; read models are derived from them. One `Events(...)` call feeds *every* read model built from those events: if the command injects both an `AccountBalance` and an `AccountStatement`, both are materialized from the same events — no read model type appears in the test.

This is distinct from `_scenario.EventScenario.Given` above: that seeds the event **log** (prior facts the handler may read or append against); this seeds the **read model state** the command observes through its injected parameters.

### Pinning a read model instance

When you would rather assert against a known value than express the events behind it, pin the instance directly. The read model type is inferred from the value:

```csharp
void Establish() =>
    _scenario.Given
        .ForEventSource(_accountId)
        .ReadModel(new AccountBalance(150m));
```

Pinned instances take precedence over seeded events for the same read model type and source. A model seeded for one source is not visible to another: resolving an unseeded source returns `null`. A registered nullable dependency can receive that null; a required dependency instead rejects the command as unavailable. See [the dependency-unavailable assertion distinction](./command-scenario.md#dependency-unavailable-is-not-a-business-rule-rejection).

**Current boundary:** this builder controls injected read model resolution. It is not a live projection subscription: appending through `EventLog` or executing a command does not automatically add those new events to this separate seed collection. If a test needs both prior log facts and injected state, arrange both explicitly. Use Chronicle's read-model scenarios to test projection behavior, rather than assuming this command harness updates every downstream view.

## Multiple events

For exact-position assertions, account for seeded events. This example assumes `CompleteOrder` appends one `OrderCompleted` after the seeded `OrderPlaced`, giving two total events:

```csharp
public class when_completing_order : Specification
{
    readonly CommandScenario<CompleteOrder> _scenario = new();
    readonly EventSourceId _orderId = EventSourceId.New();
    CommandResult _result = default!;

    Task Establish() =>
        _scenario.EventScenario.Given
            .ForEventSource(_orderId)
            .Events(new OrderPlaced("item-1", 3));

    async Task Because() =>
        _result = await _scenario.Execute(new CompleteOrder(_orderId));

    [Fact] void should_succeed() =>
        _result.ShouldBeSuccessful();

    [Fact] Task should_have_two_events_including_the_seed() =>
        _scenario.EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));

    [Fact] Task should_have_appended_completed_event() =>
        _scenario.EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the extension provides

When `Cratis.Arc.Chronicle.Testing` is referenced, `ChronicleCommandScenarioExtender` registers the following services automatically:

- `IEventTypes` → discovered from the assemblies loaded in the test process (same convention used in production)
- `IEventLog` → backed by the real in-process Chronicle kernel (no server required)
- `IEventSequence` → the event scenario's in-process sequence
- `IReadModels` → resolves a command's injected read models from the state seeded with the `Given` builder — by projecting seeded events on demand, or from a pinned instance — enabling direct read model dependencies in handlers, validators, and `Provide` methods

It also populates the scenario context, exposed through C# 14 extension properties:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `Given` | `CommandScenarioChronicleGivenBuilder<TCommand>` | The given builder for seeding read model state — `ForEventSource(id).Events(...)` or `.ReadModel(...)` |
| `EventScenario` | `EventScenario` | The full scenario, including the `Given` builder for seeding the event log |
| `EventLog` | `IEventLog` | Shortcut to `EventScenario.EventLog` for Chronicle's own assertion helpers |
| `EventSequence` | `IEventSequence` | Shortcut to `EventScenario.EventSequence` for Chronicle's assertion helpers |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | Accumulated captured appends, including event-log setup, used by scenario-level `ShouldHaveAppendedEvent` and `ShouldHaveTailSequenceNumber` |
