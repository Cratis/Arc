---
title: Cratis Arc stream completion
description: Return CompleteStream from a Cratis Arc command or reactor to close a named stream after commit and reject later appends to that stream.
---

Return `CompleteStream` from a Cratis Arc command when a period or process has ended and its stream must accept no more events. Arc commits the command's returned events first, then closes the stream. Your handler does not need to inject `IEventLog`.

Choose completion to stop future writes, not to protect a decision from concurrent changes. Chronicle's [Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) checks whether an append missed an event; it does not close the stream. A [definition-routed command](./event-source-definitions.md) already declares the route completion uses. Follow [the per-process scenario](./event-source-definition-scenarios.md#a-stream-per-process) to apply this to customer onboarding.

Event-source-scoped completion requires the `Cratis.Arc.Chronicle` integration and Chronicle kernel 19.39 or later. An older kernel fails the completion; Arc does not fall back to closing every source. Import `Cratis.Arc.Chronicle.Streams` for the completion value.

## Complete the command's routed stream

This command closes `completion / period` only for its event source `Id`. Another entity using the same stream type and id stays open.

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

A `Default` or unset stream id cannot be completed for any stream type through Arc; commands receive a validation failure and reactors receive a side-effect failure.

## Understand the completion boundary

To close only one entity's stream, give the command a declared key, a specified id from `ICanProvideEventSourceId`, or an event source id in the handler's response. Arc also narrows the completion by the resolved event source type when one is declared. You do not need to repeat the entity id in a stream-id template just to keep other entities' streams open.

To close a period for everyone, use a command without a declared or response-supplied event source id. This includes `EventSourceId.Unspecified`: completion closes that stream type and id across the entire event log. An id Arc generates internally for a keyless command does not narrow the completion, and per-event routing overrides do not change its owner. Check this distinction before using completion on a keyless command.

Arc first commits the command's events, then calls Chronicle to complete the stream. If the append or commit fails, Arc does not complete it. Nested commands wait for the outer command's commit. Completing an already completed stream succeeds, making a repeated completion safe.

:::caution[Appending and completing are not atomic]
Arc performs append and completion as separate operations without an expected-tail guard. Another writer can append between the successful commit and completion. If completion fails or the process stops in that window, the final events remain committed and the stream can remain open. A failed command result does not mean those events were rolled back. Retrying only the completion is safe; blindly retrying a command that also emits events can duplicate them.
:::

Any later command appending to a completed stream receives a validation failure with Chronicle's message naming the closed stream, rather than an internal-server error. Other completion refusals fail the command with a message identifying the target stream.

## Return a completion from a reactor

A reactor can return `CompleteStream` directly or as `Task<CompleteStream>`. Null parts use the **observed event's** stream type and id, not side-effect metadata declared on the reactor. Completion always includes the observed event's event source id and event source type, even when you override the stream type or id. This complete reactor type uses the event declared above:

```csharp
using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Reactors;

public class CompleteObservedPeriod : IReactor
{
    public CompleteStream PeriodCompleted(PeriodCompleted @event) => new();
}
```

Redelivery tolerates `AlreadyCompleted`; other refusals become reactor side-effect failures. If the reaction must append final events as well as complete the stream, return a command that declares both. A reactor cannot mix this Arc completion with events in a tuple or collection. Replay also applies the completion. Use Chronicle's `[OnceOnly]` only when you intend the handler to skip replay.

## Assert completion in a command scenario

Import `Cratis.Arc.Chronicle.Testing.Commands`. This excerpt assumes a `CommandScenario<CompletePeriod>` named `scenario` and an event source id named `id`:

```csharp
await scenario.Execute(new CompletePeriod(id));
await scenario.ShouldHaveAppendedEvent<CompletePeriod, PeriodCompleted>(id);
await scenario.ShouldHaveCompletedStream("completion", "period");
await scenario.ShouldHaveCompletedStream(new Cratis.Chronicle.EventSequences.ClosedStreamScope(
    EventSourceId: id, EventStreamType: "completion", EventStreamId: "period"));
```

`scenario.CompletedStreams` exposes successful completions as `(EventStreamType, EventStreamId)` pairs, including repeated completions that Chronicle reports as already completed. The pair assertion accepts either scoped or all-sources completions; the `ClosedStreamScope` overload asserts the exact owner and routing dimensions. Both work with protected decision scenarios.

See [transactional commands](transactional-commands.md) for event commit behavior and [reactor command side effects](../reactors/command-side-effects.md) for returning commands from reactors.
