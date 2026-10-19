---
title: How event source definition routing works
description: How Arc resolves the event source definition, stream, stream id and concurrency scope for a command's append, and what it hands to Chronicle.
---

This page follows one command from its `[EventSource<TSource>(stream)]` attribute to the append Chronicle receives. Read it when a command appends to a different stream than you expected, fails with a routing exception, or conflicts less (or more) than you expected. For the declarations themselves, see [Route a command through an event source definition](./event-source-definitions.md).

## From the attribute to a route

Arc builds the command's routing in the command context before `Handle()` runs. Four things are resolved separately and then combined into one `EventRoute`.

| Part | Resolved from | When |
| --- | --- | --- |
| Event source definition | `[EventSource<TSource>]`, looked up in the event store's discovered definitions | When the command context is built |
| Event source type and stream type | The definition's name and the named stream; the stream type is `All` when no stream is named | With the definition |
| Event source id | The command's [event source id conventions](./events.md#event-source-id-resolution) | When the command context is built |
| Stream id | `[EventStreamId]`, a template in it, or `ICanProvideEventStreamId`; the default stream id when none | A constant or interface when the context is built; a template on first use after validation |

When the context is built, Arc:

1. Reads the one `[EventSource<TSource>]` on the command type.
2. Resolves `TSource` from the discovered definitions. An unregistered type throws `UnknownEventSource`.
3. Looks up the named stream. A name the definition does not declare throws `EventRoutingContradictsEventSource`.
4. Compares the definition with `[EventSourceType]` and `[EventStreamType]` on the command. A value that differs from the definition's source name, or from the stream's name (`All` without a stream), throws `EventRoutingContradictsEventSource`.
5. Stores the definition type, the stream name, the source type, the stream type and the concurrency dimensions in the command context.

The concurrency dimensions are the stream's own when it declares any, otherwise the event source's. Chronicle owns that rule in `EventSourceDefinition.ConcurrencyFor`.

The stream id is not part of the definition. A template is resolved on first use, after authorization and validation, so a validator can reject a missing property before the template throws. `CommandContext.GetEventRoute()` returns the combined route, and `IStreamReads.ForCommand()` and `CompleteStream` use it. `IEventRoutes.For<TSource>(stream)` builds the same route outside a command, with the default stream id until you call `WithStreamId(...)`.

## From the route to an append

When `Handle()` returns events, Arc appends them through the definition. The append carries the definition type, the stream name, the stream id and the event source id, so Chronicle records the definition name in `EventContext.EventSource` and the resolved source and stream types on the event. Chronicle rejects a stream the definition does not declare.

Per-event routing changes this for one returned event:

- An `EventForEventSourceId` that sets `EventSource` and `EventStream` appends through that definition and stream.
- One that sets only `EventStream` uses the command's definition and that stream.
- One that sets the legacy `EventSourceType` or `EventStreamType` strings appends by those strings and does not use the definition. Arc rejects an undeclared stream string with `EventStreamDoesNotBelongToEventSource` when it names a known definition, and the framework's analyzers [ARCCHR0020](../code-analysis/ARCCHR0020.md) and [ARCCHR0021](../code-analysis/ARCCHR0021.md) flag the literal forms.
- `EventStreamId` and `Subject` set on the wrapper always win over the command.

[Per-event routing](./events.md#per-event-routing) has the full rules.

## The concurrency scope Arc passes

Arc decides per returned event which scope, if any, accompanies the append. The first match applies.

| Situation | Scope passed with the append |
| --- | --- |
| The command returns `EventsWithConcurrencyScopes` | The returned scopes, which replace everything below |
| A command attribute declares `concurrency: true` | A scope built from those attributes' dimensions at the strategy's expected tail |
| The command's definition declares no dimensions | None; the event sequence applies its configured strategy |
| A plain event, or a collection of plain events, and the command's definition declares dimensions | A scope for the event source id, narrowed by the declared stream type, stream id and event source type, at the strategy's expected tail |
| An `EventForEventSourceId` routed through a definition, and the command's definition declares dimensions | None from Arc; the event sequence derives the guard from that event's own definition and stream |
| An `EventForEventSourceId` routed by legacy strings, and the command's definition declares dimensions | A scope built from that event's own routing |

Three details follow from this:

- **Which dimensions the scope carries depends on the path.** The scope Chronicle derives from a definition carries exactly the declared dimensions, so a definition without `EventSourceId` guards the stream across event sources. The scope Arc builds itself for plain returned events always carries the event source id and adds the declared stream type, stream id and source type. Declare `EventSourceId` on the definition to make both paths agree.
- **One guard per route.** When several returned events target the same id through a definition, Chronicle derives one guard per distinct route and sends them together in one batch; a violation of any of them refuses the whole batch. Events that need the same guard share one. This needs a Chronicle kernel 19.84 or later: against an older kernel the client throws `IncompatibleConcurrencyScopesForEventSource` before anything is appended. Events that override the definition with legacy routing and need different guards for one id keep every distinct guard: Arc sends them all, in event order, and Chronicle requires each one to hold. Guards for the same stream and event types collapse to one. Mixing such legacy guards with a guard Chronicle derives from a definition for the same id is rejected by Arc with the same exception, because Chronicle stops deriving once it is handed a scope for the id. Split the command, or return `EventsWithConcurrencyScopes` with the guard you want.
- **Timing.** For plain events, Arc resolves the expected tail while it handles the response, after `Handle()`. For an event whose guard Chronicle derives, the tail is resolved when the unit of work commits. In both cases it is not necessarily the revision `Handle()` read. The first append into an empty scope is checked only when the client's concurrency options opt in.

A stream decision avoids the timing gap: `IStreamReads` captures the tail before it reads the stream and builds the exact scope, with the event source id, event source type, stream type and stream id from the route (and no stream id for `StreamType(...)`). `StreamDecision.Append(...)` returns it in `EventsWithConcurrencyScopes`, which replaces the derived scopes. When the stream is empty, the scope expects no matching event. The definition's dimensions are not applied to it: the exact scope always carries the route's source, stream type and stream id.

## How this maps to the Dynamic Consistency Boundary

A scope is the append condition: it names the events the append must not have missed (a query) and the position the decision was based on. In Chronicle the stream metadata plays the role of DCB tags, and an event source definition is the typed way to choose which dimensions guard an append. The dimensions choose the query; the expected tail is the position. Because the query never names event types, a definition scope guards the whole source or stream, a coarse, aggregate-like boundary: right for a cohesive history, too wide when unrelated events share the stream. See [Chronicle's Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/) for the model and [choosing a mechanism](/chronicle/dynamic-consistency-boundary/choosing/) for the guards. A definition gives the query a name and a place to live. It does not capture the position at the time of your read; a stream decision, a protected decision or an exact scope does.

## Diagnose a routing problem

| Symptom | Cause |
| --- | --- |
| `UnknownEventSource` when the command runs | The definition is not discovered by the Chronicle client for this event store |
| `EventRoutingContradictsEventSource` | The stream name is undeclared, or `[EventSourceType]` or `[EventStreamType]` disagrees with the definition |
| `AmbiguousEventStreamId` | A non-empty `[EventStreamId]` and `ICanProvideEventStreamId` are both declared |
| `UnknownEventStreamIdTemplateProperty`, `InvalidEventStreamIdTemplate`, `EventStreamIdTemplatePartMissing` | A template names a property that is not a public readable instance property, has malformed braces, or resolves a part or the whole id to blank or the default |
| `EventStreamDoesNotBelongToEventSource` | A returned wrapper names an undeclared stream by legacy strings on a definition-routed command |
| `IncompatibleConcurrencyScopesForEventSource` | Returned events for one event source id mix legacy-routed guards with a guard Chronicle derives from a definition |
| `StreamDecisionRequiresSpecificRoute` | `IStreamReads` was asked to read a route without an event source id, a stream type or a stream id |
| No conflict where you expected one | The check is the default append check, or the definition declares no dimensions, or the dimensions do not include the one that differs; see [what the definition guards](./event-source-definitions.md#what-the-definition-guards) |

## Where to go next

- [Scenarios](./event-source-definition-scenarios.md): complete commands for each stream shape.
- [Concurrency](./concurrency.md): the attribute-based scope and the exact-scope rules.
- [Stream decisions](./stream-decisions.md) and [completing streams](./completing-streams.md): what builds on the route.
- [Choosing how a command's rule holds](../consistency/index.md).
