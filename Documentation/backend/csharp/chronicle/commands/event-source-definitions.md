---
title: Route a command through an event source definition
description: Declare the event source and stream a command appends to with [EventSource<TSource>], choose the stream id, and know which concurrency boundary the definition gives the append.
---

An event source definition is a class that names a kind of event source and the streams it owns. A command that declares `[EventSource<TSource>(stream)]` appends its events through that definition instead of through loose strings. The definition supplies the event source type, the stream type and the concurrency dimensions, and Arc rejects a stream the definition does not declare.

Use this guide when a command should append to a named stream and you want its routing and its concurrency boundary declared once, in one typed place, instead of as strings on each command. A definition gives the append a **boundary**; it does not make a decision hold. [What the definition guards](#what-the-definition-guards) says exactly what you get, and [choosing how a command's rule holds](../consistency/index.md) tells you what to add when the rule depends on state.

The definition is a Chronicle concept; see [Chronicle event sources](/chronicle/event-sources/) for how Chronicle registers and stores it. This page covers what Arc does with it.

## Declare the definition and the command

The definition is a class implementing `IEventSource`, marked `[EventSource]` and one `[EventStream]` per stream. Without an explicit name, the event source name is the class name with a trailing `EventSource` removed.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

[EventSource("Account")]
[EventStream("Transactions", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType)]
public class AccountEventSource : IEventSource;

[EventType]
public record TransactionRecorded(decimal Amount);

[Command]
[EventSource<AccountEventSource>("Transactions")]
public record RecordTransaction(EventSourceId AccountId, decimal Amount)
{
    public TransactionRecorded Handle() => new(Amount);
}
```

The event is appended with:

- event source type `Account`, from the definition name;
- event stream type `Transactions`, from the stream you named;
- the `AccountId` as event source id, resolved by the usual [event source id conventions](./events.md#event-source-id-resolution);
- the default stream id, because the command declares none.

The command's namespace needs `Cratis.Arc.Chronicle.Commands` for `[EventSource<TSource>]` and `Cratis.Chronicle.EventSources` for the definition types. The definition must be discovered by the Chronicle client like any other artifact; an unregistered definition type fails when the command runs.

A command declares at most one `[EventSource<TSource>]`. The stream argument is optional: without it the command routes through the definition's source type with the default stream type.

## Choose the stream

A stream is the part of an event source's history that one kind of decision shares. Pick the stream by asking which events must be checked against each other when two commands run at once.

**Recommended:** one definition per kind of entity, one `[EventStream]` per history that is decided separately, and the event source id as the entity's id. Name the stream on the command with `[EventSource<TSource>(stream)]`. The reason: the definition is the single place that says which streams exist and which dimensions guard them, and Arc rejects a typo. Alternatives, in order of preference: the command key and default stream with no definition, when one history per entity is enough; string attributes (`[EventStreamType]`), which ARCCHR0013 flags once a matching definition exists; explicit scopes, when the decision must be bound to a revision it read.

| You want | Declare | Stream id |
| --- | --- | --- |
| One history per entity, everything for it together | One stream, or none | Leave unset; the event source id identifies the entity |
| Independent histories of one entity, such as `Onboarding` and `Transactions` | One `[EventStream]` per history | Leave unset |
| One history per entity and period, owner or process | One stream | A template or `ICanProvideEventStreamId`, see below |

`EventStream` on the attribute must name a stream the definition declares. A typo or an undeclared name fails when the command's context is built, with `EventRoutingContradictsEventSource`, instead of appending to a stream nobody reads. [Scenarios](./event-source-definition-scenarios.md) shows each shape as a complete command.

## Choose the stream id

The definition names the stream. The stream id is separate: it picks one concrete stream within the stream type, and the definition does not supply it. Declare it on the command with the existing stream id mechanisms:

- `[EventStreamId("constant")]` for a fixed id.
- `ICanProvideEventStreamId` when code computes it.
- A template in `[EventStreamId("{Owner}:{Period}")]` when it is composed from command properties.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

[EventSource("Report")]
[EventStream("Approval", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class ReportEventSource : IEventSource;

[EventType]
public record ReportApproved;

[Command]
[EventSource<ReportEventSource>("Approval")]
[EventStreamId("{Owner}:{Period}")]
public record ApproveReport(EventSourceId ReportId, string Owner, DateOnly Period)
{
    public ReportApproved Handle() => new();
}
```

For `Owner = "acme"` and `Period = 2026-10-01`, the stream id is `acme:2026-10-01`. A template part is a public instance property. Concepts use their underlying value, `DateOnly` uses `yyyy-MM-dd`, and `{{` and `}}` are literal braces; the [full format table](./concurrency.md#template-event-stream-ids) lists the rest. Arc refuses unknown properties, blank parts, malformed braces and an id that resolves to the default, and it resolves the template after validation, so a validator can reject a missing part first.

Use the same function when you read or seed the stream: `EventStreamIdTemplate.ResolveFor(command)` returns the id exactly as the pipeline resolves it.

Combining a non-empty `[EventStreamId]` with `ICanProvideEventStreamId` is ambiguous and throws `AmbiguousEventStreamId`.

## What the definition guards

Chronicle's Dynamic Consistency Boundary (DCB) selects the events an append must not have missed with *tags*. In Chronicle the stream metadata plays that role: event source id, event source type, stream type and stream id. An event source definition is the typed way to choose which of those dimensions guard an append. The concurrency dimensions on the definition decide which events a returned event is checked against. `[EventSource]` can declare dimensions for every stream, and each `[EventStream]` can declare its own; a stream that declares dimensions replaces the source's for that stream, and a stream that declares none inherits the source's. The dimensions are a combination of:

| Dimension | The append is checked against events with the same |
| --- | --- |
| `EventSourceId` | Event source id. Leave it out only when the guard should span every event source that writes the stream. |
| `EventSourceType` | Event source type, the definition name |
| `EventStreamType` | Stream type, the stream name |
| `EventStreamId` | Stream id, from `[EventStreamId]` |

With no dimensions declared, the definition adds no guard of its own and the event sequence applies its configured strategy as it does for any append.

Include `EventSourceId` unless you want a stream-wide guard across sources: a definition-derived guard without it is checked against all sources' events with the same stream type and stream id.

Four limits matter:

- **It is the default append check, not a decision guard.** Arc resolves the expected tail while it processes the returned events, after `Handle()` has run. A concurrent append between your read and that moment is part of the new expected tail and does not conflict. The first append into an empty scope is checked only when the client's concurrency options opt in. To bind a decision to what it read, use a [stream decision](./stream-decisions.md), a [protected decision](../read-models/injecting-into-commands.md#decision-reads-for-event-dependent-commands) or [exact scopes](./events.md#events-with-exact-concurrency-scopes).
- **An explicit choice wins.** `concurrency: true` on `[EventStreamId]`, `[EventStreamType]` or `[EventSourceType]` selects the dimensions from those attributes, and a returned `EventsWithConcurrencyScopes` replaces every derived scope. The definition stays the policy for every command that does not override it.
- **It guards the whole source or stream, whatever the event type.** The scope narrows by event source id, source type, stream type and stream id, never by event type. In DCB terms that is a coarse, aggregate-like boundary: any new event in the guarded stream conflicts, related or not. That is the right shape when the events in the stream are all relevant to the decisions made against it, for example a short history per entity or per period. When unrelated event types share the stream, give the decision its own stream, or use a [protected decision](../read-models/injecting-into-commands.md#decision-reads-for-event-dependent-commands), whose scope narrows to the event types its model folds. A [stream decision](./stream-decisions.md) has the same whole-stream boundary and also reads the whole stream.
- **One event source per event.** The boundary is an event source id plus the dimensions above. Chronicle does not tag one event with several entities, so a fact about two entities is two events, one per event source, appended in one atomic batch. See [Chronicle's Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) for how a concurrency scope maps to the boundary and [when to use which mechanism](/chronicle/dynamic-consistency-boundary/choosing/).

[How routing works](./event-source-definition-routing.md) follows a single append from the attribute to the scope Chronicle receives.

## Reading and completing the routed stream

The route the command declares is the same route its reads and completions use:

- [`IStreamReads.ForCommand()`](./stream-decisions.md) reads the command's stream at a captured tail and returns a `StreamDecision` whose `Append(...)` carries an exact scope.
- [`CompleteStream`](./completing-streams.md) closes the command's stream after its events commit.
- `IEventRoutes` resolves a route for any definition outside a command; see [per-event routing](./events.md#per-event-routing).

## Migrate from string routing

Commands without a definition keep working. To move one over:

1. Declare an `IEventSource` with `[EventSource("Name")]` and an `[EventStream]` for each stream type the command used. Use the strings you already store as the names so existing events and observers match.
2. Replace `[EventSourceType("Name")]` and `[EventStreamType("Stream")]` on the command with `[EventSource<TSource>("Stream")]`. Keep `[EventStreamId]` and `ICanProvideEventStreamId`; the definition does not replace them.
3. Move `concurrency: true` to dimensions on the definition. While a command still carries the flag, the flag chooses the dimensions and the definition's are ignored for it.

[ARCCHR0013](../code-analysis/index.md#arcchr0013-use-the-event-source-definition) (Info) finds the commands to migrate: when `[EventSourceType]` or `[EventStreamType]` on a command or aggregate root names a source that a definition in the compilation declares, it suggests `[EventSource<TSource>(stream)]`. It has no code fix, because the attributes can carry `concurrency: true` flags that the definition's dimensions do not reproduce.

Do not keep a string attribute that contradicts the definition. `[EventSourceType]` or `[EventStreamType]` with a value other than the definition's source or stream fails with `EventRoutingContradictsEventSource` rather than append events whose strings disagree with the definition.

### Wrapper-named streams

A returned `EventForEventSourceId` can name its route with `EventSourceType` and `EventStreamType` strings. That still works, but the event is appended by those strings: it loses `EventContext.EventSource` and the definition's concurrency dimensions. Two analyzers help you migrate:

- [ARCCHR0021](../code-analysis/ARCCHR0021.md) (Info) finds literal strings that spell a declared stream. Its code fix replaces them with `EventSource = typeof(Definition)` and `EventStream = "stream"`, or you can use `IEventRoutes`. Review the concurrency change first, because the definition's dimensions now apply to that event.
- [ARCCHR0020](../code-analysis/ARCCHR0020.md) (Warning) finds a literal stream name that the definition does not declare, which is usually a typo. At runtime a definition-routed command rejects such a wrapper with `EventStreamDoesNotBelongToEventSource` before it enrolls or appends anything.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventSource("Account")]
[EventStream("Transactions")]
[EventStream("Adjustments")]
public class AccountEventSource : IEventSource;

[EventType]
public record BalanceAdjusted(decimal Amount);

[Command]
[EventSource<AccountEventSource>("Transactions")]
public record AdjustAccount(EventSourceId AccountId, decimal Amount)
{
    // Before: new EventForEventSourceId(AccountId, new BalanceAdjusted(Amount))
    //         { EventSourceType = "Account", EventStreamType = "Adjustments" }
    public EventForEventSourceId Handle(IEventRoutes routes) =>
        routes.For<AccountEventSource>("Adjustments").Route(AccountId, new BalanceAdjusted(Amount));
}
```

## Where to go next

- [Scenarios](./event-source-definition-scenarios.md): a stream per entity, per owner and period, and per process, with a stream decision and a completed stream.
- [How routing works](./event-source-definition-routing.md): how Arc resolves the definition, stream, stream id and scope.
- [Choosing how a command's rule holds](../consistency/index.md) and its [FAQ](../consistency/faq.md#event-source-definitions-and-stream-decisions): where definitions fit among the guards.
- Chronicle: [event sources](/chronicle/event-sources/), [Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/), [choosing a mechanism](/chronicle/dynamic-consistency-boundary/choosing/), [scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) and [FAQ](/chronicle/dynamic-consistency-boundary/faq/).
