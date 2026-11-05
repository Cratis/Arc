---
title: Cratis Arc event source routing
description: Trace a Cratis Arc command from its event source definition to the resolved route and concurrency scope, then diagnose routing errors.
---

Cratis Arc resolves `[EventSource<TSource>(stream)]` into the route used to append a command's events. Follow that route here when events land in an unexpected stream, a routing exception stops the command, or a concurrency check covers the wrong history. To declare a new route, start with [event source definitions](./event-source-definitions.md).

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

Check the stream's `Concurrency` first when diagnosing the selected dimensions. If the stream declares any, they replace the source's dimensions; otherwise it inherits the source's. Chronicle implements this in `EventSourceDefinition.ConcurrencyFor`.

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

Routing and concurrency are separate choices: the route says where the event goes; the scope says which history must remain unchanged. For automatic append handling without read protection `On`, use the first matching row below. Under `On`, the transaction handles the guard as described in [how read protection works](../consistency/how-it-works.md).

| Situation | Scope passed with the append |
| --- | --- |
| The command returns `EventsWithConcurrencyScopes` | The returned scopes, which replace everything below |
| A command attribute declares `concurrency: true` | A scope built from those attributes' dimensions at the strategy's expected tail |
| The command's definition declares no dimensions | None; the event sequence applies its configured strategy |
| A plain event, or a collection of plain events, and the command's definition declares dimensions | A scope for the event source id, narrowed by the declared stream type, stream id and event source type, at the strategy's expected tail |
| An `EventForEventSourceId` routed through a definition, and the command's definition declares dimensions | None from Arc; the event sequence derives the guard from that event's own definition and stream |
| An `EventForEventSourceId` routed by legacy strings, and the command's definition declares dimensions | A scope built from that event's own routing |

For a per-entity boundary, include `EventSourceId` on the definition. Chronicle's definition-derived scope carries exactly the declared dimensions, so omitting that dimension can guard across sources. Arc's scope for plain returned events always includes the target source id. Including it explicitly makes both paths agree.

When several returned events target different routes for the same source id, each distinct guard must hold for the batch to append. Chronicle derives guards for definition-routed events; Arc sends the guards for legacy-routed events in event order. Equivalent guards are shared. Several guards for one id require Chronicle kernel 19.84 or later; an older kernel causes `IncompatibleConcurrencyScopesForEventSource` before anything is appended.

Do not mix legacy-derived guards and Chronicle-derived definition guards for the same source id. Arc rejects that combination with `IncompatibleConcurrencyScopesForEventSource`, because supplying a scope stops Chronicle deriving one for that id. Split the command or return `EventsWithConcurrencyScopes` containing the intended guard.

The capture time also differs. Arc resolves the expected tail for plain events while handling the response, after `Handle()`. Chronicle resolves a definition-derived guard when the unit of work commits. Neither is necessarily the revision the handler read. The client's concurrency options must opt in to automatic first-append checking for an empty scope.

A stream decision avoids the timing gap: `IStreamReads` captures the tail before it reads the stream and builds the exact scope, with the event source id, event source type, stream type and stream id from the route (and no stream id for `StreamType(...)`). `StreamDecision.Append(...)` returns it in `EventsWithConcurrencyScopes`, which replaces the derived scopes. When the stream is empty, the scope expects no matching event. The definition's dimensions are not applied to it: the exact scope always carries the route's source, stream type and stream id.

## How this maps to the Dynamic Consistency Boundary

A Dynamic Consistency Boundary (DCB) has two parts: the query selecting relevant events and the position those events were read at. Chronicle uses stream metadata as DCB tags. A definition chooses the query's dimensions, but automatic tail capture does not supply the position of your earlier read.

For example, a definition scoped to an account's `Transactions` stream checks every event in that stream, not just payments. That works when every event matters to the decision. If unrelated events share the stream, the check may be too broad. A stream decision binds the position to the read but still guards the whole stream; a protected read model can narrow by the event types it folds.

Use [Chronicle's Dynamic Consistency Boundary guide](/chronicle/dynamic-consistency-boundary/) for the model and [choosing a mechanism](/chronicle/dynamic-consistency-boundary/choosing/) to select the guard for your rule.

## Diagnose a routing problem

| Symptom | Cause |
| --- | --- |
| `UnknownEventSource` when the command runs | The definition is not discovered by the Chronicle client for this event store |
| `EventRoutingContradictsEventSource` | The stream name is undeclared, or `[EventSourceType]` or `[EventStreamType]` disagrees with the definition |
| `AmbiguousEventStreamId` | A non-empty `[EventStreamId]` and `ICanProvideEventStreamId` are both declared |
| `UnknownEventStreamIdTemplateProperty`, `InvalidEventStreamIdTemplate`, `EventStreamIdTemplatePartMissing` | A template names a property that is not a public readable instance property, has malformed braces, or resolves a part or the whole id to blank or the default |
| `EventStreamDoesNotBelongToEventSource` | A returned wrapper names an undeclared stream by legacy strings on a definition-routed command |
| `IncompatibleConcurrencyScopesForEventSource` | Returned events for one event source id mix legacy-routed guards with a guard Chronicle derives from a definition, or need several guards against a Chronicle kernel older than 19.84 |
| `StreamDecisionRequiresSpecificRoute` | `IStreamReads` was asked to read a route without an event source id, a stream type or a stream id |
| No conflict where you expected one | The check is the default append check, or the definition declares no dimensions, or the dimensions do not include the one that differs; see [what the definition guards](./event-source-definitions.md#what-the-definition-guards) |

## Where to go next

- [Scenarios](./event-source-definition-scenarios.md): complete commands for each stream shape.
- [Concurrency](./concurrency.md): the attribute-based scope and the exact-scope rules.
- [Stream decisions](./stream-decisions.md) and [completing streams](./completing-streams.md): what builds on the route.
- [Choosing how a command's rule holds](../consistency/index.md).
