---
title: Stream decisions
description: Read a routed stream and return events guarded at the tail used by the decision.
---

When a command decides from events in one stream, inject `IStreamReads` in `Provide()` and return `decision.Append(...)` from `Handle()`. The returned events carry the read route and an exact concurrency scope. A competing append to that stream after the read rejects the command with a concurrency validation failure, including when both commands attempt the first append.

This guide assumes Chronicle is [configured for the application](../index.md). `IStreamReads` and `IEventRoutes` resolve by Arc's service conventions; no additional registration is needed.

## Read the command's stream

Declare the route on the command. `ForCommand()` uses the event source id and route already resolved by the pipeline, including a [template stream id](./concurrency.md#template-event-stream-ids).

```csharp
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[Command]
[EventSourceType("reports")]
[EventStreamType("approval")]
[EventStreamId("{Owner}:{Month}")]
public record ApproveReport(EventSourceId ReportId, string Owner, string Month)
{
    public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand();

    public EventsWithConcurrencyScopes Handle(StreamDecision decision) =>
        decision.Has<ReportApproved>()
            ? decision.Append([])
            : decision.Append(new ReportApproved());
}

[EventType]
public record ReportApproved;
```

The empty append represents an **identical retry**: approval was already recorded and there is nothing new to record. It returns no events and no scopes and succeeds without checking whether the stream changed again. Do not use this branch to hide a missing prerequisite or a rejected action, and do not return a nullable event. Reject invalid requests through validation.

`StreamDecision.Events` contains `AppendedEvent` values; `EventsOf<TEvent>()` selects payloads and `Has<TEvent>()` checks for a type. The reader captures `Tail` **before** fetching events and discards events beyond that tail. `IsEmpty` means the tail was unavailable, and the scope uses `ExpectingNoMatchingEvent()` to protect the first append. Source type, stream type and stream id filters are identical for the read and guard.

## Reuse a definition-backed route

For a command declaring `[EventSource<TSource>(stream)]`, `ForCommand()` also retains the event source definition and its selected stream. If you need a route outside the current command, inject `IEventRoutes` and resolve it from the same discovered registry. This excerpt assumes your application declares an `IEventSource` named `ReportSource` with an `approval` stream:

```csharp
EventRoute route = routes.For<ReportSource>("approval").WithStreamId("owner:2026-10");
StreamDecision decision = await reads.Stream(reportId, route);
EventsWithConcurrencyScopes response = decision.Append(new ReportApproved());
```

Here `routes` is `IEventRoutes`, `reads` is `IStreamReads` and `reportId` is an `EventSourceId`. `EventRoute.For(definition, stream)` is the pure alternative when you already have an `EventSourceDefinition`. An unknown stream raises `EventRoutingContradictsEventSource`, just as a command declaration does.

`CommandContext.GetEventRoute()` exposes the pipeline's resolved route. For ordinary returned events, `route.Route(id, fact)` and `route.Route(id, facts)` produce wrappers stamped with all routing dimensions and, when present, the definition. A route built with `new EventRoute(sourceType, streamType, streamId)` keeps string-based routing available.

## Guard every stream id of a type

Call `reads.StreamType(id, route)` when a decision depends on all streams of one type for the event source. Its scope omits the stream id dimension, so an append to a sibling stream id conflicts too. `decision.Append(...)` still writes to the **specific stream id** in `route`.

Both read modes require a specified event source id, a specific stream type and a specific append stream id. `EventSourceId.Unspecified`, `EventStreamType.All`, and `EventStreamId.Default` or `NotSet` raise `StreamDecisionRequiresSpecificRoute`.

## Keep one exact scope per source

Chronicle's `EventsWithConcurrencyScopes` labels scopes by event source id: one scope per id per command. It cannot carry separate guards for two stream ids of the same source. Use the stream-type-wide variant when that boundary matches the decision, or split the command. Do not combine two independent stream decisions for the same id and assume both tails are guarded.

Exact scopes replace automatic concurrency capture. Remove `concurrency: true` from string routing attributes on a command returning `EventsWithConcurrencyScopes`; retain the routing values. A shared event source definition's concurrency policy stays on the definition, and the exact returned scope intentionally takes precedence. See [concurrency](./concurrency.md#exact-scopes-replace-automatic-capture).

## Seed and interfere on the same route

Enable decision reads before seeding so the command and its reads share one in-process event log. This excerpt uses `ApproveReport` and `ReportApproved` from the first example:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

await using var scenario = new CommandScenario<ApproveReport>().UseDecisionReads();
var id = EventSourceId.New();
var route = new EventRoute("reports", "approval", "owner:2026-10");
scenario.Given.ForEventSource(id).OnRoute(route).Events(new ReportApproved());
scenario.AppendConcurrently(id, route, new ReportApproved());
```

For a definition, use `scenario.EventRoutes.For<ReportSource>("approval").WithStreamId(...)`. Routed seeding in the legacy read-model mode is refused with `RoutedEventSeedingRequiresDecisionReads` because that mode cannot retain stream routing.

A nonempty decision append conflicts with a matching queued competitor; a competitor on another stream id succeeds for `Stream()` and conflicts for `StreamType()`. Execute a command that produces a new fact to test that guard: the identical-retry branch deliberately returns no scope.
