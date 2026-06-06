---
uid: Arc.Testing.ChronicleExtension
---
# Chronicle

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

## Basic Usage

Use the same `CommandScenario<TCommand>` class as for non-Chronicle commands. When the Chronicle testing package is present, four extension properties are available directly on the scenario:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `EventScenario` | `EventScenario` | The full scenario object — use for seeding via `Given` |
| `EventLog` | `IEventLog` | The in-memory event log — use for appending and assertions |
| `EventSequence` | `IEventSequence` | The same instance as `EventLog` — use with Chronicle's assertion helpers |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | The events captured during command execution |

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

## Seeding Pre-existing Events with `Given`

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

    [Fact] Task should_not_have_appended_a_second_event() =>
        _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

Seed events before calling `Execute` so they are present when the command handler runs.

## EventLog Assertion Helpers

Chronicle provides a set of assertion helpers that extend `IEventSequence` directly. Call them on `_scenario.EventLog` or `_scenario.EventSequence` after `Execute`. For the full list of available assertions, see <xref:Chronicle.Testing.Events.Assertions>.

## Testing Commands That Use EventForEventSourceId

When a command handler returns `EventForEventSourceId` or `IEnumerable<EventForEventSourceId>`, events are appended to different event sources than the command's own event source id. The standard `EventLog.ShouldHaveAppendedEvent<T>(sequenceNumber)` helpers work against a single sequence and cannot filter by event source id. For these cases use the `CommandScenario`-level assertion helpers, which capture events during execution via the client-side `AppendOperations` observable.

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

> **Sequence numbering applies here too**: `ShouldHaveTailSequenceNumber` checks the highest sequence number across all captured events. Two events means a tail of `1` (zero-based).

The `AppendedEvents` extension property gives you the raw list if you need to write custom assertions:

```csharp
[Fact]
void should_have_exactly_two_events() =>
    _scenario.AppendedEvents.Count.ShouldEqual(2);
```

## Testing Commands That Take Read Model Dependencies

`CommandScenario<TCommand>` also supports direct read model dependencies in command handlers and validators, using the same convention-based registration as runtime (`IProjectionFor<T>` and model-bound projections).

In tests, you can keep things deterministic by overriding `IReadModels` and returning the instance you want for the current event source id:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

var scenario = new CommandScenario<UseReadModelDependencyCommand>();
var readModels = Substitute.For<IReadModels>();

readModels
    .GetInstanceById(typeof(AccountBalanceReadModel), Arg.Any<ReadModelKey>(), default)
    .Returns(Task.FromResult<object>(new AccountBalanceReadModel(42m)));

scenario.Services.Replace(ServiceDescriptor.Singleton<IReadModels>(readModels));
```

## Multiple Events

When a command appends several events, assert each one by its sequence number:

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

    [Fact] Task should_have_appended_two_events() =>
        _scenario.EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));

    [Fact] Task should_have_appended_completed_event() =>
        _scenario.EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the Extension Provides

When `Cratis.Arc.Chronicle.Testing` is referenced, `ChronicleCommandScenarioExtender` registers the following services automatically:

- `IEventTypes` → discovered from the assemblies loaded in the test process (same convention used in production)
- `IEventLog` → backed by the real in-process Chronicle kernel (no server required)
- `IEventSequence` → the same in-process instance
- `IReadModels` and convention-registered read model services → enabling direct read model dependencies in handlers and validators

It also populates the scenario context with an `EventScenario` instance, which is exposed through C# 14 extension properties:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `EventScenario` | `EventScenario` | The full scenario, including the `Given` builder for seeding events |
| `EventLog` | `IEventLog` | Shortcut to `EventScenario.EventLog` for Chronicle's own assertion helpers |
| `EventSequence` | `IEventSequence` | Shortcut to `EventScenario.EventSequence` for Chronicle's assertion helpers |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | All events captured during command execution, used by `ShouldHaveAppendedEvent` and `ShouldHaveTailSequenceNumber` |
