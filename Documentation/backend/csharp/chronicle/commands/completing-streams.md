---
title: Completing streams
description: Return a stream completion from a command or reactor to prevent further events in that stream.
---

Return `CompleteStream` when a stream must accept no more events. Arc completes it after the command's returned events commit; your handler does not need to inject `IEventLog`.

This requires the `Cratis.Arc.Chronicle` integration. Import `Cratis.Arc.Chronicle.Streams` for the completion value.

## Complete the command's routed stream

This command uses a named stream type and stream id. The event source id identifies the command's entity; the completion uses the stream metadata, not the entity id.

```csharp
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[EventType]
public record PeriodCompleted;

[Command]
[EventStreamType("completion")]
[EventStreamId("period")]
public record CompletePeriod(EventSourceId Id)
{
    public (PeriodCompleted, CompleteStream) Handle() => (new(), new());
}
```

`CompleteStream()` uses the command context's resolved stream type and id, including metadata supplied by an event source definition. You can also return it alone when no final event is needed. It is a side effect, not the command's response value: a tuple can still carry a separate response. Putting the completion before the event in the tuple does not change execution order.

To select a different stream, supply either or both parts:

```csharp
new CompleteStream(EventStreamType: "completion", EventStreamId: "another-period")
```

Each omitted part uses the corresponding command route value. Returning events with per-event routing overrides does not change these defaults; name the completion's target explicitly when it differs from the command route.

The default stream (`All` with `Default` or an unset id) cannot be completed. Arc rejects that declaration as a validation failure and rolls back the command's returned events.

## Understand the completion boundary

A completion is keyed by **stream type and stream id across the entire event log**. It is not scoped to an event source id or event source type. Choose stream ids that do not accidentally close another entity's stream.

Arc first commits the command's events, then calls Chronicle to complete the stream. If the append or commit fails, Arc does not complete it. Nested commands wait for the outer command's commit. Completing an already completed stream succeeds, making a repeated completion safe.

:::caution[Appending and completing are not atomic]
Chronicle does not offer an atomic append-and-complete operation. Another writer can append between the successful commit and completion. If completion fails or the process stops in that window, the final events remain committed and the stream can remain open. A failed command result does not mean those events were rolled back. Retrying only the completion is safe; blindly retrying a command that also emits events can duplicate them.
:::

Any later command appending to a completed stream receives a validation failure with Chronicle's message naming the closed stream, rather than an internal-server error. Other completion refusals fail the command with a message identifying the target stream.

## Return a completion from a reactor

A reactor can return `CompleteStream` directly or as `Task<CompleteStream>`. Null parts use the **observed event's** stream type and id, not side-effect metadata declared on the reactor. This complete reactor type uses the event declared above:

```csharp
using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Reactors;

public class CompleteObservedPeriod : IReactor
{
    public CompleteStream PeriodCompleted(PeriodCompleted @event) => new();
}
```

Redelivery tolerates `AlreadyCompleted`. Other refusals become reactor side-effect failures. Chronicle does not support mixing this Arc completion with events in a tuple or collection. If a reaction must append final events and then complete, return a command that declares both instead. Replay also applies the completion; use Chronicle's `[OnceOnly]` only when the handler should deliberately skip replay.

## Assert completion in a command scenario

Import `Cratis.Arc.Chronicle.Testing.Commands`. This excerpt assumes a `CommandScenario<CompletePeriod>` named `scenario` and an event source id named `id`:

```csharp
await scenario.Execute(new CompletePeriod(id));
await scenario.ShouldHaveAppendedEvent<CompletePeriod, PeriodCompleted>(id);
await scenario.ShouldHaveCompletedStream("completion", "period");
```

`scenario.CompletedStreams` exposes successful completions as `(EventStreamType, EventStreamId)` pairs, including repeated completions that Chronicle reports as already completed. The same assertion works with protected decision scenarios.

See [transactional commands](transactional-commands.md) for event commit behavior and [reactor command side effects](../reactors/command-side-effects.md) for returning commands from reactors.
