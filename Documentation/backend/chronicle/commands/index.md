---
title: Commands
description: How an Arc command becomes appended events — return shapes, event source identity, subject, and concurrency scoping.
---

Chronicle extends the [Arc command pipeline](../../commands/index.md) with event sourcing behavior. The same [model-bound](../../commands/model-bound/index.md) command patterns work here — `[Command]` records, `Handle()` methods, `CommandValidator` — with Chronicle adding automatic event appending, event source identity resolution, and metadata-driven concurrency control.

## What Chronicle Adds

A model-bound command handler in the Chronicle context can return events directly from `Handle()`, and Chronicle appends those events to the event log automatically. This keeps command handlers focused on decisions and domain logic rather than event log plumbing.

Chronicle also resolves the event source identity, event stream metadata, and concurrency scope from the command record itself — either by convention or via explicit attributes and interfaces. This means the same record that defines your command's shape also carries all the information Chronicle needs to append events correctly.

## What `Handle()` returns decides what happens

Chronicle inspects the value `Handle()` returns and appends accordingly. Anything it doesn't recognize as an event (or event metadata) becomes the command's **response** to the caller — so a tuple lets you append a fact _and_ hand something back without ever calling the event log yourself.

| `Handle()` returns | What Chronicle does |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `void` / `Task` | No return-driven append; aggregates or explicit appends can still write events. |
| An `[EventType]` event | Appended to the resolved event source's log. |
| `IEnumerable<object>` of events | Each is appended, in order. |
| `EventForEventSourceId` (or a collection of them) | Appended to the event source id carried _in the value_, overriding the resolved one — for writing to a different or several streams. |
| `EventsWithConcurrencyScopes` | Appends ordered cross-source events with exact, independently labeled concurrency scopes in the command transaction. |
| Tuple `(event, result)` | The event is appended; the other element is returned to the caller as the response. |
| Tuple containing `EventSourceId` and an event, in either order | The `EventSourceId` sets the stream; the event is appended. See [Returning EventSourceId](./returning-event-source-id.md). |
| Tuple `(Guid, event)` on a keyless command | The `Guid` is only the response. Chronicle generates a different event source id; [ARCCHR0010](../code-analysis/ARCCHR0010.md) asks whether the Guid was intended as stream identity. |
| Tuple `(event, Subject)` | The event is appended; the `Subject` is attached as [compliance metadata](./subject.md), not returned. |
| `Result<TResult, TError>` (e.g. `Result<TEvent, ValidationResult>`) | A failure rejects pending transactional events; a success is unwrapped. Earlier immediate or explicit commits remain. |

The rule of thumb: **return the fact that happened.** The event source id (see [Resolving EventSourceId](../resolving-event-source-id.md)) decides which stream it lands on.

## Topics

| Topic | Description |
| --------------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| [Events](./events.md) | Returning events from commands and how Chronicle appends them automatically, including stream metadata. |
| [Setting Subject](./subject.md) | Supplying a compliance subject on the command or by returning it from `Handle()`. |
| [Returning EventSourceId](./returning-event-source-id.md) | Explicitly deciding the event source id by returning it from a command tuple. |
| [Transactional commands](./transactional-commands.md) | Atomic enrollment, immediate appends, nested commands, and aggregate commit boundaries. |
| [Concurrency](./concurrency.md) | Declaring a concurrency scope on commands using metadata attributes and interfaces. |
