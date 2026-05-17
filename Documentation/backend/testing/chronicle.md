---
uid: Arc.Testing.ChronicleExtension
---
# Chronicle

When `Cratis.Arc.Chronicle.Testing` (or the `Cratis.Testing` meta-package) is referenced, `CommandScenario<TCommand>` is automatically extended with an in-memory event scenario. No separate class or base type is needed.

The extension is wired via `ChronicleCommandScenarioExtender`, which implements `ICommandScenarioExtender` and is discovered automatically by `CommandScenario<TCommand>` at construction time using the Cratis type discovery system.

## Package

```xml
<PackageReference Include="Cratis.Arc.Chronicle.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Testing" />
```

## Basic Usage

Use the same `CommandScenario<TCommand>` class as for non-Chronicle commands. When the Chronicle testing package is present, three extension properties are available directly on the scenario:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `EventScenario` | `EventScenario` | The full scenario object — use for seeding via `Given` |
| `EventLog` | `IEventLog` | The in-memory event log — use for appending and assertions |
| `EventSequence` | `IEventSequence` | The same instance as `EventLog` — use with Chronicle's assertion helpers |

```csharp
public class when_registering_author
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();

    [Fact]
    public async Task should_succeed()
    {
        var result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        result.ShouldBeSuccessful();
    }

    [Fact]
    public async Task should_have_appended_registered_event()
    {
        await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        await _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(EventSequenceNumber.First);
    }
}
```

## Seeding Pre-existing Events with `Given`

Use `_scenario.EventScenario.Given` to append events to the in-memory event log *before* the command runs. Call `ForEventSource` with the event source identifier, then pass the pre-existing events to `Events`:

```csharp
public class when_registering_author_with_same_name
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();

    [Fact]
    public async Task should_not_succeed()
    {
        await _scenario.EventScenario.Given.ForEventSource(AuthorId.New()).Events(new AuthorRegistered("Jane Austen"));
        var result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        result.ShouldNotBeSuccessful();
    }

    [Fact]
    public async Task should_not_have_appended_a_second_event()
    {
        await _scenario.EventScenario.Given.ForEventSource(AuthorId.New()).Events(new AuthorRegistered("Jane Austen"));
        await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    }
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

public class when_migrating_customer_to_new_id
{
    readonly CommandScenario<MigrateCustomerToNewId> _scenario = new();
    readonly EventSourceId _oldId = EventSourceId.New();
    readonly EventSourceId _newId = EventSourceId.New();

    [Fact]
    public async Task should_have_appended_migrated_event_for_new_id() =>
        await _scenario.Execute(new MigrateCustomerToNewId(_oldId, _newId))
            .ContinueWith(_ => _scenario.ShouldHaveAppendedEvent<MigrateCustomerToNewId, CustomerMigrated>(_newId));

    [Fact]
    public async Task should_reference_old_id_in_event() =>
        await _scenario.Execute(new MigrateCustomerToNewId(_oldId, _newId))
            .ContinueWith(_ => _scenario.ShouldHaveAppendedEvent<MigrateCustomerToNewId, CustomerMigrated>(
                _newId,
                e => e.OldCustomerId == _oldId));
}
```

### Example: Multiple cross-source events (fund transfer)

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

public class when_transferring_funds
{
    readonly CommandScenario<TransferFunds> _scenario = new();
    readonly EventSourceId _fromAccount = EventSourceId.New();
    readonly EventSourceId _toAccount = EventSourceId.New();

    async Task Execute() =>
        await _scenario.Execute(new TransferFunds(_fromAccount, _toAccount, 250m));

    [Fact]
    public async Task should_have_debited_from_account()
    {
        await Execute();
        await _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsDebited>(_fromAccount);
    }

    [Fact]
    public async Task should_have_credited_to_account()
    {
        await Execute();
        await _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsCredited>(_toAccount);
    }

    [Fact]
    public async Task should_have_debited_correct_amount()
    {
        await Execute();
        await _scenario.ShouldHaveAppendedEvent<TransferFunds, FundsDebited>(_fromAccount, e => e.Amount == 250m);
    }

    [Fact]
    public async Task should_have_appended_two_events()
    {
        await Execute();
        await _scenario.ShouldHaveTailSequenceNumber<TransferFunds>(1ul);
    }
}
```

> **Sequence numbering applies here too**: `ShouldHaveTailSequenceNumber` checks the highest sequence number across all captured events. Two events means a tail of `1` (zero-based).

The `AppendedEvents` extension property gives you the raw list if you need to write custom assertions:

```csharp
[Fact]
public async Task should_have_exactly_two_events()
{
    await Execute();
    _scenario.AppendedEvents.Count.ShouldEqual(2);
}
```

## Multiple Events

When a command appends several events, assert each one by its sequence number:

```csharp
public class when_completing_order
{
    readonly CommandScenario<CompleteOrder> _scenario = new();

    [Fact]
    public async Task should_succeed()
    {
        await _scenario.EventScenario.Given.ForEventSource(OrderId.New()).Events(new OrderPlaced("item-1", 3));
        var result = await _scenario.Execute(new CompleteOrder());
        result.ShouldBeSuccessful();
    }

    [Fact]
    public async Task should_have_appended_two_events()
    {
        await _scenario.EventScenario.Given.ForEventSource(OrderId.New()).Events(new OrderPlaced("item-1", 3));
        await _scenario.Execute(new CompleteOrder());
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));
    }

    [Fact]
    public async Task should_have_appended_completed_event()
    {
        await _scenario.EventScenario.Given.ForEventSource(OrderId.New()).Events(new OrderPlaced("item-1", 3));
        await _scenario.Execute(new CompleteOrder());
        await _scenario.EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
    }
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the Extension Provides

When `Cratis.Arc.Chronicle.Testing` is referenced, `ChronicleCommandScenarioExtender` registers the following services automatically:

- `IEventTypes` → discovered from the assemblies loaded in the test process (same convention used in production)
- `IEventLog` → backed by the real in-process Chronicle kernel (no server required)
- `IEventSequence` → the same in-process instance

It also populates the scenario context with an `EventScenario` instance, which is exposed through C# 14 extension properties:

| Property | Type | Purpose |
| -------- | ---- | ------- |
| `EventScenario` | `EventScenario` | The full scenario, including the `Given` builder for seeding events |
| `EventLog` | `IEventLog` | Shortcut to `EventScenario.EventLog` for Chronicle's own assertion helpers |
| `EventSequence` | `IEventSequence` | Shortcut to `EventScenario.EventSequence` for Chronicle's assertion helpers |
| `AppendedEvents` | `IReadOnlyList<AppendedEventWithResult>` | All events captured during command execution, used by `ShouldHaveAppendedEvent` and `ShouldHaveTailSequenceNumber` |

