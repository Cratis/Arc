---
uid: Arc.Testing.ChronicleExtension
---
# Chronicle Extension

When `Cratis.Arc.Chronicle.Testing` (or the `Cratis.Testing` meta-package) is referenced, `CommandScenario<TCommand>` is automatically extended with an in-memory event log. No separate class or base type is needed.

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

Use the same `CommandScenario<TCommand>` class as for non-Chronicle commands. When the Chronicle testing package is present, the `EventLog` and `Given` extension properties are available:

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

Use `_scenario.Given` to append events to the in-memory event log *before* the command runs. Call `ForEventSource` with the event source identifier, then pass the pre-existing events to `Events`:

```csharp
public class when_registering_author_with_same_name
{
    readonly CommandScenario<RegisterAuthor> _scenario = new();

    [Fact]
    public async Task should_not_succeed()
    {
        await _scenario.Given.ForEventSource(AuthorId.New()).Events(new AuthorRegistered("Jane Austen"));
        var result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        result.ShouldNotBeSuccessful();
    }

    [Fact]
    public async Task should_not_have_appended_a_second_event()
    {
        await _scenario.Given.ForEventSource(AuthorId.New()).Events(new AuthorRegistered("Jane Austen"));
        await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    }
}
```

Seed events before calling `Execute` so they are present when the command handler runs.

## EventLog Assertion Helpers

The assertion helpers extend `IEventSequence` directly — they are provided by Chronicle's `EventSequenceShouldExtensions` class in the `Cratis.Chronicle.Testing` package. Because `EventLogForTesting` implements `IEventSequence`, you can call them on `_scenario.EventLog` after `Execute`.

### `ShouldHaveTailSequenceNumber`

Asserts the total number of events in the log by checking the tail sequence number.

`EventSequenceNumber.First` corresponds to sequence number `0`, meaning exactly one event has been appended.

```csharp
// Assert exactly one event was appended total
await _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
```

### `ShouldHaveAppendedEvent<TEvent>`

Asserts that the event at a given sequence number has the expected type.

```csharp
// Assert the first event is an AuthorRegistered
await _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(EventSequenceNumber.First);
```

### `ShouldHaveAppendedEvent<TEvent>` with a validator

Pass a validator action to inspect the event's content:

```csharp
await _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(
    EventSequenceNumber.First,
    e => e.Name.ShouldEqual("Jane Austen"));
```

### `ShouldHaveAppendedEvent<TEvent>` with event source and validator

When the command appends events for a known event source, you can scope the assertion to that source:

```csharp
var authorId = new AuthorId(Guid.Parse("..."));

await _scenario.EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(
    EventSequenceNumber.First,
    authorId,
    e => e.Name.ShouldEqual("Jane Austen"));
```

### Assertion helper reference

| Method | Asserts that... |
| ------ | --------------- |
| `ShouldHaveTailSequenceNumber(expected)` | The event log tail equals `expected` |
| `ShouldHaveAppendedEvent<TEvent>(seq)` | Event at `seq` has type `TEvent` |
| `ShouldHaveAppendedEvent<TEvent>(seq, validator)` | Event at `seq` has type `TEvent` and passes `validator` |
| `ShouldHaveAppendedEvent<TEvent>(seq, eventSourceId, validator)` | Event at `seq` for `eventSourceId` has type `TEvent` and passes `validator` |

All helpers throw `EventSequenceAssertionException` with a descriptive message on failure.

## Multiple Events

When a command appends several events, assert each one by its sequence number:

```csharp
public class when_completing_order
{
    readonly CommandScenario<CompleteOrder> _scenario = new();

    [Fact]
    public async Task should_succeed()
    {
        await _scenario.Given.ForEventSource(OrderId.New()).Events(new OrderPlaced("item-1", 3));
        var result = await _scenario.Execute(new CompleteOrder());
        result.ShouldBeSuccessful();
    }

    [Fact]
    public async Task should_have_appended_two_events()
    {
        await _scenario.Given.ForEventSource(OrderId.New()).Events(new OrderPlaced("item-1", 3));
        await _scenario.Execute(new CompleteOrder());
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));
    }

    [Fact]
    public async Task should_have_appended_completed_event()
    {
        await _scenario.Given.ForEventSource(OrderId.New()).Events(new OrderPlaced("item-1", 3));
        await _scenario.Execute(new CompleteOrder());
        await _scenario.EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
    }
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the Extension Provides

When `Cratis.Arc.Chronicle.Testing` is referenced, `ChronicleCommandScenarioExtender` registers the following services automatically:

- `IEventTypes` → discovered from the assemblies loaded in the test process (same convention used in production)
- `IEventLog` → in-memory `EventLogForTesting`
- `IEventSequence` → the same `EventLogForTesting` instance

It also populates the scenario context with `EventLogForTesting` and `EventScenarioGivenBuilder`, which are exposed through the `EventLog` and `Given` extension properties.
