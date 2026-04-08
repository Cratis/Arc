---
uid: Arc.Testing.ChronicleCommandScenario
---
# Chronicle Command Scenarios

`ChronicleCommandScenario<TCommand>` extends [`CommandScenario<TCommand>`](./command-scenario.md) with an in-memory event log. Commands that append events through Chronicle — returning events from `Handle()` — can be tested entirely in-process: no external Chronicle server, no MongoDB instance.

## Package

```xml
<PackageReference Include="Cratis.Arc.Chronicle.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Testing" />
```

## Basic Usage

Instantiate `ChronicleCommandScenario<TCommand>` as a field in your test class. Call `Execute` inside each `[Fact]` and assert on `EventLog` afterwards:

```csharp
public class when_registering_author
{
    readonly ChronicleCommandScenario<RegisterAuthor> _scenario = new();

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

Use the `Given` property to append events to the in-memory event log *before* the command runs. This is the primary way to set up the aggregate or read model state the command handler will see:

```csharp
public class when_registering_author_with_same_name
{
    readonly ChronicleCommandScenario<RegisterAuthor> _scenario = new();

    [Fact]
    public async Task should_not_succeed()
    {
        await _scenario.Given.Event(AuthorId.New(), new AuthorRegistered("Jane Austen"));
        var result = await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        result.ShouldNotBeSuccessful();
    }

    [Fact]
    public async Task should_not_have_appended_a_second_event()
    {
        await _scenario.Given.Event(AuthorId.New(), new AuthorRegistered("Jane Austen"));
        await _scenario.Execute(new RegisterAuthor("Jane Austen"));
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
    }
}
```

Seed events before calling `Execute` so they are present when the command handler runs.

## EventLog Assertion Helpers

All helpers live on `EventLogForTesting` as extension methods in `EventLogForTestingShouldExtensions`. Use them after calling `Execute` to verify which events were appended and what their contents are.

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

All helpers throw `EventLogAssertionException` with a descriptive message on failure.

## Multiple Events

When a command appends several events, assert each one by its sequence number:

```csharp
public class when_completing_order
{
    readonly ChronicleCommandScenario<CompleteOrder> _scenario = new();

    [Fact]
    public async Task should_succeed()
    {
        await _scenario.Given.Event(OrderId.New(), new OrderPlaced("item-1", 3));
        var result = await _scenario.Execute(new CompleteOrder());
        result.ShouldBeSuccessful();
    }

    [Fact]
    public async Task should_have_appended_two_events()
    {
        await _scenario.Given.Event(OrderId.New(), new OrderPlaced("item-1", 3));
        await _scenario.Execute(new CompleteOrder());
        await _scenario.EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));
    }

    [Fact]
    public async Task should_have_appended_completed_event()
    {
        await _scenario.Given.Event(OrderId.New(), new OrderPlaced("item-1", 3));
        await _scenario.Execute(new CompleteOrder());
        await _scenario.EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
    }
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the Scenario Provides

`ChronicleCommandScenario<TCommand>` registers the following services on top of what `CommandScenario<TCommand>` provides:

- `IEventLog` → in-memory `EventLogForTesting`
- `IEventSequence` → the same `EventLogForTesting` instance
- `IEventTypes` → discovered from the assemblies loaded in the test process (same convention used in production)

The Arc Chronicle infrastructure for event appending, response value handlers, and event source identity resolution is wired against these in-memory implementations automatically.
