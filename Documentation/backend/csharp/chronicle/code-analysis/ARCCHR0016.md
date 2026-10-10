---
title: 'ARCCHR0016: Concurrency flag is ignored when the handler returns exact concurrency scopes'
description: A stream-metadata attribute sets concurrency true on a command whose Handle method returns EventsWithConcurrencyScopes, so the flag has no effect.
---

## Rule

`[EventSourceType]`, `[EventStreamType]` and `[EventStreamId]` take a `concurrency` flag that adds the attribute to the concurrency scope Arc derives for the command's events. When `Handle()` returns `EventsWithConcurrencyScopes`, the returned scopes are used exactly as they are and nothing is derived from the attributes, so `concurrency: true` does nothing.

The rule fires on a model-bound `[Command]` whose public instance `Handle()` returns `EventsWithConcurrencyScopes` directly, in a `Task<>` or `ValueTask<>`, inside a `Result<>`/`OneOf<>` branch, or as an element of a tuple. It reports the `concurrency` argument whether it is passed positionally or by name. `concurrency: false` and a missing argument are not reported.

## Severity

Warning

## Example

### Violation

```csharp
[Command]
[EventStreamId("settlements", concurrency: true)] // ARCCHR0016
public record SettleFunds(EventSourceId AccountId)
{
    public Task<EventsWithConcurrencyScopes> Handle(IStreamReads streams) => ...;
}
```

### Fix

```csharp
[Command]
[EventStreamId("settlements")]
public record SettleFunds(EventSourceId AccountId)
{
    public Task<EventsWithConcurrencyScopes> Handle(IStreamReads streams) => ...;
}
```

## Quick fix

**Remove concurrency: true** deletes the argument and keeps the rest of the attribute. Fix All is supported.

## Event source definitions are not reported

A definition (`[EventSource<T>]` with concurrency dimensions) is a shared policy that other writers also follow, so the command cannot remove it. An exact scope, for example from `StreamDecision.Append`, intentionally replaces it for this append. The rule therefore only looks at the attributes on the command itself.

## See also

- [Returning events: events with exact concurrency scopes](../commands/events.md#events-with-exact-concurrency-scopes)
- [Stream decisions](../commands/stream-decisions.md)
- [Concurrency](../commands/concurrency.md)
