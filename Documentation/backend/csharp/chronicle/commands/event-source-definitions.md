---
title: Cratis Arc event source definitions
description: Route Cratis Arc commands through typed event source definitions to share stream names and append policies without repeating routing strings.
---

An event source definition gives Cratis Arc commands a shared, typed declaration of an entity's event streams. Use `[EventSource<TSource>(stream)]` when several commands should use the same stream names and append policy. Arc resolves the route from the definition and rejects undeclared stream names.

The definition supplies the event source type, stream type and concurrency dimensions: the fields that select which history an append checks for changes. It does not capture the revision a command read. If a rule depends on existing state, pair the route with a guard such as a stream decision or read protection. [What the definition guards](#what-the-definition-guards) explains that distinction.

Definitions belong to Chronicle; [Chronicle event sources](/chronicle/event-sources/) covers registration and storage. This guide shows how to use them from Arc commands.

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

Start with one definition per kind of entity and use the entity's id as the event source id. Declare a stream for each history that needs a separate check, then name it on the command. For example, account transactions and onboarding can use different streams so a write to one need not conflict with a write to the other.

If one unnamed history per entity is enough, the command key and default stream need no definition. Existing string attributes also work, but ARCCHR0013 suggests using a definition when one matches. Explicit concurrency scopes solve a different problem: tying the append to the revision used by a decision. They can be used alongside definition-based routing.

| You want | Declare | Stream id |
| --- | --- | --- |
| One history per entity, everything for it together | One stream, or none | Leave unset; the event source id identifies the entity |
| Independent histories of one entity, such as `Onboarding` and `Transactions` | One `[EventStream]` per history | Leave unset |
| One history per entity and period, owner or process | One stream | A template or `ICanProvideEventStreamId`, see below |

`EventStream` on the attribute must name a stream the definition declares. A typo or an undeclared name fails when the command's context is built, with `EventRoutingContradictsEventSource`, instead of appending to a stream nobody reads. [Scenarios](./event-source-definition-scenarios.md) shows each shape as a complete command.

## Choose the stream id

The definition names a stream type, such as `Approval`. A stream id selects an instance of that type, such as one owner's monthly approval. The definition does not supply this id; choose how the command supplies it:

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

For `Owner = "acme"` and `Period = 2026-10-01`, the stream id is `acme:2026-10-01`. A template part is a public instance property. Concepts use their underlying value, `DateOnly` uses `yyyy-MM-dd`, and `{{` and `}}` are literal braces; the [full format table](./concurrency.md#template-event-stream-ids) lists the rest.

Arc refuses unknown properties, blank parts, malformed braces and an id that resolves to the default. It normally resolves the template after validation. A per-stream model injected into a validator needs the route earlier, so Arc resolves it before constructing that validator and refuses a blank part without reading the model.

Use the same function when you read or seed the stream: `EventStreamIdTemplate.ResolveFor(command)` returns the id exactly as the pipeline resolves it.

Combining a non-empty `[EventStreamId]` with `ICanProvideEventStreamId` is ambiguous and throws `AmbiguousEventStreamId`.

## What the definition guards

Choose concurrency dimensions by asking which writes should compete. To check every write for one account, select `EventSourceId`. To check each report period separately, also select `EventStreamType` and `EventStreamId`, as in the approval example above.

These metadata fields act as the tags that select history in Chronicle's Dynamic Consistency Boundary (DCB). `[EventSource]` sets the default dimensions. A stream with its own dimensions replaces that default; a stream without them inherits it.

| Dimension | The append is checked against events with the same |
| --- | --- |
| `EventSourceId` | Event source id. Leave it out only when the guard should span every event source that writes the stream, and only on a definition-routed `EventForEventSourceId`; see below. |
| `EventSourceType` | Event source type, the definition name |
| `EventStreamType` | Stream type, the stream name |
| `EventStreamId` | Stream id, from `[EventStreamId]` |

With no dimensions declared, the definition adds no guard of its own and the event sequence applies its configured strategy as it does for any append.

For a per-entity check, include `EventSourceId` explicitly. The return shape affects how the scope is built when the command's definition declares dimensions:

- For a definition-routed `EventForEventSourceId`, alone or in a collection, Chronicle derives a scope with exactly the declared dimensions. Set `EventSource` on the wrapper or build it with `IEventRoutes` to use this path.
- For plain events, plain collections and legacy-string wrappers, Arc builds the scope with the target source id plus the declared stream type, stream id and source type.

Omitting `EventSourceId` can therefore create a cross-source check on the first path, but not the second. If that is intentional, return definition-routed events. Otherwise include the dimension so both paths agree. [How routing works](./event-source-definition-routing.md) lists each return path.

The automatic check captures the expected tail, or last matching position, after `Handle()` has run. A write arriving between your read and that capture can become part of the expected tail rather than cause a conflict. Automatic checks of the first append into empty history also require the client's concurrency option. For an approval that must happen once, use a [stream decision](./stream-decisions.md), a read model parameter under [read protection](../consistency/getting-started.md) `On`, or [exact scopes](./events.md#events-with-exact-concurrency-scopes) to guard the revision actually read.

A definition scope checks all event types in the selected history. That suits a short stream where every event matters to the decision. If unrelated event types share the stream, consider a separate stream or a read-protected model whose guard follows the event types it folds. A stream decision also covers, and reads, the whole stream.

Command-level `concurrency: true` flags override the definition's dimension choice. Returned `EventsWithConcurrencyScopes` replace automatic scopes altogether. Keep the definition as the shared policy and override it only where a command needs a different guard.

Each event still belongs to one event source. A fact about two entities needs two events, one per source, appended in one atomic batch. [Chronicle's Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) and [mechanism guide](/chronicle/dynamic-consistency-boundary/choosing/) explain how to guard such decisions.

[How routing works](./event-source-definition-routing.md) follows a single append from the attribute to the scope Chronicle receives.

### Declare concurrency sparingly

Declare `Concurrency` when a competing write anywhere in that boundary should reject the append, as in a ledger, numbering or approval stream. Do not add it just because commands share a route: it can create conflicts between unrelated writes without protecting the state used by a decision.

Under read protection `On`, a declaration can widen a read's guard at commit. For a read of the same source, the merged guard keeps only dimensions whose values agree and covers any event type. A declaration without `EventSourceId` stays separate and does not protect the state you read. Review [the merge example](../consistency/how-it-works.md#declared-concurrency-on-a-definition) before combining these policies. Chronicle's [CHR0062](/chronicle/code-analysis/chr0062/) reports declarations without `EventSourceId`.

## Reading and completing the routed stream

The route the command declares is the same route its reads and completions use:

- [`IStreamReads.ForCommand()`](./stream-decisions.md) reads the command's stream at a captured tail and returns a `StreamDecision` whose `Append(...)` carries an exact scope.
- [`CompleteStream`](./completing-streams.md) closes the command's stream after its events commit.
- `IEventRoutes` resolves a route for any definition outside a command; see [per-event routing](./events.md#per-event-routing).

## Migrate from string routing

Commands without a definition keep working. To move one over:

1. Declare an `IEventSource` with `[EventSource("Name")]` and an `[EventStream]` for each stream type the command used. Use the strings you already store as the names so existing events and observers match.
2. Replace `[EventSourceType("Name")]` and `[EventStreamType("Stream")]` on the command with `[EventSource<TSource>("Stream")]`. Keep `[EventStreamId]` and `ICanProvideEventStreamId`; the definition does not replace them.
3. Review each `concurrency: true` flag before moving its policy to the definition. Preserve the intended dimensions rather than enabling all of them. While a command keeps a flag, its attribute dimensions override the definition's for that command.

[ARCCHR0013](../code-analysis/ARCCHR0013.md) (Info) finds the commands to migrate: when `[EventSourceType("Name")]` on a command or aggregate root names a source that a definition in the compilation declares, it suggests `[EventSource<TSource>]`, or `[EventSource<TSource>("stream")]` when an `[EventStreamType]` names a stream the definition declares. It stays silent when the stream is not declared, and for a type that already has `[EventSource<TSource>]`. It has no code fix, because the attributes can carry `concurrency: true` flags that the definition's dimensions do not reproduce.

Do not keep a string attribute that contradicts the definition. `[EventSourceType]` or `[EventStreamType]` with a value other than the definition's source or stream fails with `EventRoutingContradictsEventSource` rather than append events whose strings disagree with the definition.

### Wrapper-named streams

A returned `EventForEventSourceId` can name its route with `EventSourceType` and `EventStreamType` strings. That still works, but the event is appended by those strings: it loses `EventContext.EventSource` and the wrapper target's own definition policy. On a definition-routed command Arc still applies the command's selected definition dimensions, and builds the scope from the wrapper's own routing strings, always including the target event source id. Two analyzers help you migrate:

- [ARCCHR0021](../code-analysis/ARCCHR0021.md) (Info) finds literal strings that spell a declared stream. Its code fix replaces them with `EventSource = typeof(Definition)` and `EventStream = "stream"`, or you can use `IEventRoutes`. Review the concurrency change first, because the guard now comes from the definition, which Chronicle derives with exactly the declared dimensions rather than Arc's scope that always includes the event source id.
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
