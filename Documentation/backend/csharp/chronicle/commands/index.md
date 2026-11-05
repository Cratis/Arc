---
title: Write events with Chronicle commands
description: Return events from Arc commands to record decisions in Chronicle, choose their event source identity, and reject writes that break a rule.
---

Cratis Chronicle's integration with Cratis Arc lets a command return the events that record its decision. Arc appends those events through the command's transaction, so a rejected command does not leave pending events behind. Use the same [model-bound commands](../../commands/model-bound/index.md) and validators as in the [Arc command pipeline](../../commands/index.md); add Chronicle routing and concurrency rules where the write needs them.

## What Chronicle Adds

Return an `[EventType]` fact from `Handle()` rather than calling the event log yourself. Arc resolves which event source owns it, adds the command's stream metadata, and enrolls it in the pending transaction. The transaction commits when the command succeeds. Immediate appends and an explicit early commit sit outside that rollback boundary; see [transactional commands](./transactional-commands.md).

The event source id identifies the entity whose history you are changing. Stream metadata selects a route within that history. A concurrency scope states which history must remain unchanged for the decision to hold. Arc can derive these from the command, or you can return explicit routing and scopes when the decision requires them.

## What `Handle()` returns decides what happens

Arc processes events, metadata, command operations and recognized validation results on the server. At most one ordinary value that no response handler consumes becomes the caller's response. A tuple can therefore carry an event and a response without making the event itself response data.

:::note[Returned work is not response data]
Return events for durable facts and [command operations](../../commands/operations/index.md) for additional inline work. Neither becomes the client response. Operations run within their supported command boundary; their external effects are not atomically committed with Chronicle events.
:::

| `Handle()` returns | What Chronicle does |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `void` / `Task` | No return-driven append; aggregates or explicit appends can still write events. |
| An `[EventType]` event | Appended to the resolved event source's log. |
| `IEnumerable<object>` of events | Each is appended, in order. |
| `EventForEventSourceId` (or a collection of them) | Appended to the id carried in each value, for writing to a different event source or several sources. |
| `EventsWithConcurrencyScopes` | Appends ordered cross-source events with exact, independently labeled concurrency scopes in the command transaction. |
| Tuple `(event, result)` with an ordinary unhandled result | The event is appended; the ordinary result becomes the caller's response. |
| Tuple containing events and command operations | Events enroll in the command transaction; operations execute before completion. At most one additional ordinary value may be the response. |
| Tuple containing `EventSourceId` and an event, in either order | The `EventSourceId` selects the event source and becomes the response. See [Returning EventSourceId](./returning-event-source-id.md). |
| Tuple `(Guid, event)` on a keyless command | The `Guid` is only the response. Chronicle generates a different event source id; [ARCCHR0010](../code-analysis/ARCCHR0010.md) asks whether the Guid was intended as stream identity. |
| Tuple `(event, EventTags)` | The tags attach to every returned event, not the response. See [Event tags](./event-tags.md). |
| Tuple `(event, Subject)` | The event is appended; the `Subject` is attached as [compliance metadata](./subject.md), not returned. |
| `Result<TResult, TError>` (e.g. `Result<TEvent, ValidationResult>`) | Arc processes the active alternative. A blocking `ValidationResult` rejects pending transactional events; an ordinary error DTO is only response data. Earlier immediate or explicit commits remain. |

Return the fact that happened when the command is valid. If a business rule forbids the action, return an explicit validation failure rather than `null` or an empty event collection. Returning no events does not tell Arc that the command failed. See [rejecting a command](./events.md#reject-a-command-without-recording-an-event) for the return contract and [Resolving EventSourceId](../resolving-event-source-id.md) for identity selection.

## Topics

| Topic | Description |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| [Events](./events.md) | Returning events from commands and how Chronicle appends them automatically, including stream metadata. |
| [Event tags](./event-tags.md) | Attaching structured named tags from command properties, handler results, and application-wide conventions. |
| [Setting Subject](./subject.md) | Supplying a compliance subject on the command or by returning it from `Handle()`. |
| [Returning EventSourceId](./returning-event-source-id.md) | Returning the new entity's identity to the caller and using it for the append. |
| [Fresh identifiers](./fresh-identifiers.md) | Rejecting a generated append target that already has events, without rerunning the handler. |
| [Commands for occurrences](./occurrence-commands.md) | Recording scheduled or lifecycle work with a receipt so redelivery can reuse its outcome. |
| [Transactional commands](./transactional-commands.md) | Atomic enrollment, immediate appends, nested commands, and aggregate commit boundaries. |
| [Concurrency](./concurrency.md) | Declaring a concurrency scope on commands using metadata attributes and interfaces. |
| [Event source definitions](./event-source-definitions.md) | Routing a command's events through a typed event source definition: streams, stream ids and the boundary it gives the append. |
| [Event source definition scenarios](./event-source-definition-scenarios.md) | A stream per entity, per owner and period, and per process, with a stream decision and a completed stream. |
| [How event source definition routing works](./event-source-definition-routing.md) | How Arc resolves the definition, stream, stream id and concurrency scope. |
| [Stream decisions](./stream-decisions.md) | Reading a routed stream and returning events guarded at the tail the decision used. |
| [Completing streams](./completing-streams.md) | Closing a stream from a command or a reactor. |
