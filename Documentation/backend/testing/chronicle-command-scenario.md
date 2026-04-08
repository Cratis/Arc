---
uid: Arc.Testing.ChronicleCommandScenario
---
# Chronicle Command Scenarios

`ChronicleCommandScenarioFor<TCommand>` extends [`CommandScenarioFor<TCommand>`](./command-scenario.md) with an in-memory event log. Commands that append events through Chronicle — returning events from `Handle()` — can be tested entirely in-process: no external Chronicle server, no MongoDB instance.

## Package

```xml
<PackageReference Include="Cratis.Arc.Chronicle.Testing" />
```

Or via the meta-package:

```xml
<PackageReference Include="Cratis.Testing" />
```

## Basic Usage

Like `CommandScenarioFor<TCommand>`, your test class inherits from the scenario class and also implements `IAsyncLifetime` for xUnit lifecycle integration. Override `InitializeAsync()` to set up state and execute the command, then assert on `EventLog`:

```csharp
public class when_registering_author
    : ChronicleCommandScenarioFor<RegisterAuthor>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync(); // builds the DI container and pipeline
        _result = await Execute(new RegisterAuthor("Jane Austen"));
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_have_appended_registered_event() =>
        await EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(EventSequenceNumber.First);
}
```

## Seeding Pre-existing Events with `Given`

Use the `Given` property to append events to the in-memory event log *before* the command runs. This is the primary way to set up the aggregate or read model state the command handler will see:

```csharp
public class when_registering_author_with_same_name
    : ChronicleCommandScenarioFor<RegisterAuthor>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await Given.Event(AuthorId.New(), new AuthorRegistered("Jane Austen"));
        await base.InitializeAsync();
        _result = await Execute(new RegisterAuthor("Jane Austen"));
    }

    [Fact] void should_not_succeed() => _result.ShouldNotBeSuccessful();

    [Fact]
    async Task should_not_have_appended_a_second_event() =>
        await EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
}
```

Seed events before calling `base.InitializeAsync()` so they are present when the command handler executes.

## EventLog Assertion Helpers

All helpers live on `EventLogForTesting` as extension methods. Use them after calling `Execute` to verify which events were appended and what their contents are.

### `ShouldHaveTailSequenceNumber`

Asserts the total number of events in the log by checking the tail sequence number.

`EventSequenceNumber.First` corresponds to sequence number `0`, meaning exactly one event has been appended.

```csharp
// Assert exactly one event was appended total
await EventLog.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);
```

### `ShouldHaveAppendedEvent<TEvent>`

Asserts that the event at a given sequence number has the expected type.

```csharp
// Assert the first event is an AuthorRegistered
await EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(EventSequenceNumber.First);
```

### `ShouldHaveAppendedEvent<TEvent>` with a validator

Pass a validator action to inspect the event's content:

```csharp
await EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(
    EventSequenceNumber.First,
    e => e.Name.ShouldEqual("Jane Austen"));
```

### `ShouldHaveAppendedEvent<TEvent>` with event source and validator

When the command appends events for a known event source, you can scope the assertion to that source:

```csharp
var authorId = new AuthorId(Guid.Parse("..."));

await EventLog.ShouldHaveAppendedEvent<AuthorRegistered>(
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
    : ChronicleCommandScenarioFor<CompleteOrder>, IAsyncLifetime
{
    CommandResult _result = null!;

    public override async Task InitializeAsync()
    {
        await Given.Event(OrderId.New(), new OrderPlaced("item-1", 3));
        await base.InitializeAsync();
        _result = await Execute(new CompleteOrder());
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact]
    async Task should_have_appended_two_events() =>
        await EventLog.ShouldHaveTailSequenceNumber(new EventSequenceNumber(1));

    [Fact]
    async Task should_have_appended_completed_event() =>
        await EventLog.ShouldHaveAppendedEvent<OrderCompleted>(new EventSequenceNumber(1));
}
```

> **Sequence numbering**: Sequence numbers are zero-based. `EventSequenceNumber.First` is `0`. The second event is `new EventSequenceNumber(1)`, the third `new EventSequenceNumber(2)`, and so on. `ShouldHaveTailSequenceNumber` reports the number of the *last* appended event — so two total events means a tail of `1`.

## What the Base Class Provides

`ChronicleCommandScenarioFor<TCommand>` registers the following additional services on top of what `CommandScenarioFor<TCommand>` provides:

- `IEventLog` → in-memory `EventLogForTesting`
- `IEventSequence` → the same `EventLogForTesting` instance
- `IEventTypes` → discovered from the assemblies loaded in the test process (same convention used in production)

The Arc Chronicle infrastructure for event appending, response value handlers, and event source identity resolution is wired against these in-memory implementations automatically.
