---
title: Guard Chronicle commands with stream decisions
description: Read a Chronicle stream at a captured revision and return events that fail to append if a competing write changes the history used by the command.
---

A stream decision keeps a command's append tied to the Cratis Chronicle history it read. Inject `IStreamReads` in `Provide()` and return `decision.Append(...)` from `Handle()`. A nonempty append carries the read route and a concurrency scope: the expected revision and the stream it protects. A competing write after that revision rejects the command, even when both writers try to create the stream's first event.

Use this when your rule examines raw events in one routed stream. The whole stream crosses the wire for each command, so keep it short. If the rule needs state folded from events instead, take a read model parameter under [read protection](../consistency/getting-started.md) `On`. For uniqueness, use a constraint. [Choosing how a command's rule holds](../consistency/index.md) compares these options; the [period-approval scenario](../consistency/scenarios/per-stream.md#a-period-approval) shows the trade-off in a command.

Cratis Arc captures the stream's tail, its latest sequence number, before fetching events and uses it to guard the append. This is Chronicle's [Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) over one stream. See [choosing a mechanism](/chronicle/dynamic-consistency-boundary/choosing/) and [Chronicle's scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) for the event-store side of that boundary.

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

The empty branch in this example accepts an already-recorded approval as an identical retry. `decision.Append([])` returns no events and no scopes; it succeeds without checking for subsequent stream changes. It does not reject the command. If approving twice is invalid in your domain, return a [validation failure](./events.md#reject-a-command-without-recording-an-event) instead. Use the same explicit rejection for missing prerequisites, not an empty or nullable event.

`StreamDecision.Events` contains `AppendedEvent` values. Use `EventsOf<TEvent>()` for payloads and `Has<TEvent>()` to check whether an event type is present.

The reader captures `Tail` before fetching events and discards anything beyond that revision. `IsEmpty` means no tail was available; `ExpectingNoMatchingEvent()` then guards the first append. The read and guard use the same source type, stream type and stream id filters.

If you need folded state on the command's own route, use a [per-stream read model](../consistency/scenarios/per-stream.md) declared with `[EventStream<TSource>(stream)]` as a plain parameter. Under `On` its read is guarded; `Off` and `Measure` read the same stream without a guard. A `DecisionRead<T>` parameter of that declared model also uses the command route. For another stream or source, use the command-aware [`IDecisionReads` stream overload](../read-models/injecting-into-commands.md#read-a-projection-within-one-stream) directly in `Handle()` and suppress ARCCHR0040 with a reason and a link to the tracking feature. Under `On`, or in a `[ProtectedDecision]` command, it enrolls in the same transaction. Models without a per-stream declaration keep their key-based injection.

## Reuse a definition-backed route

For a command declaring [`[EventSource<TSource>(stream)]`](./event-source-definitions.md), `ForCommand()` also retains the event source definition and its selected stream. If you need a route outside the current command, inject `IEventRoutes` and resolve it from the same discovered registry. This excerpt assumes your application declares an `IEventSource` named `ReportSource` with an `approval` stream:

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

## Combine a stream decision with a read

An exact stream scope must cover every protected read that depends on it. Under read protection `On`, reading a whole-source model of the same event source adds a broader requirement than the stream scope can satisfy. Arc refuses that combination with `ExplicitScopeDoesNotCoverReads`; [ARCCHR0036](../code-analysis/ARCCHR0036.md) also reports it at build time. Read a different source, or use `IDecisionReads` with the same stream dimensions.

## Keep one exact scope per source

Arc's current `EventsWithConcurrencyScopes` response handler reads one scope per event source id through the response's `ConcurrencyScopes` accessor. Returning separate guards for two stream ids under the same source label therefore fails rather than protecting both tails. Use the stream-type-wide variant when it matches the rule, or split the command. Do not combine independent stream decisions for the same id and assume both are guarded.

Exact scopes replace automatic concurrency capture. Remove `concurrency: true` from string routing attributes on a command returning `EventsWithConcurrencyScopes`; retain the routing values. A shared event source definition's concurrency policy stays on the definition, and the exact returned scope intentionally takes precedence. See [concurrency](./concurrency.md#exact-scopes-replace-automatic-capture).

## Seed and interfere on the same route

Start with a spec that seeds the routed history and checks the command's outcome. The stream-decision dependency selects one in-process event log before seeding, so the command reads the same log you arrange. This excerpt uses `ApproveReport` and `ReportApproved` from the first example:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;

await using var scenario = new CommandScenario<ApproveReport>();
var id = EventSourceId.New();
var route = new EventRoute("reports", "approval", "owner:2026-10");
scenario.Given.ForEventSource(id).OnRoute(route).Events(new ReportApproved());
```

For a definition, use `scenario.EventRoutes.For<ReportSource>("approval").WithStreamId(...)`. Routed seeding in the legacy read-model mode is refused with `RoutedEventSeedingRequiresDecisionReads` because that mode cannot retain stream routing.

[Scenarios for each stream shape](./event-source-definition-scenarios.md#decide-against-the-stream) show a stream decision on a definition-routed command, and [how routing works](./event-source-definition-routing.md#the-concurrency-scope-arc-passes) explains the scope it passes.

Check both outcomes in your application specs: a new approval appends its event, while the example's identical-retry branch succeeds without another event. If your rule rejects repeat approvals, assert the validation failure instead. Arc's framework specs cover interference with the captured revision; application specs need not simulate competing writers. For an explicit framework-verification need, use the [advanced concurrency note](../../testing/chronicle.md#protected-decision-scenarios).
