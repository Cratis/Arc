---
title: 'ARCCHR0021: Use event source definition routing'
description: Legacy wrapper route strings match a declared definition stream but omit its definition and concurrency policy.
---

## Rule

Legacy `EventSourceType` / `EventStreamType` strings on `EventForEventSourceId` spell out a declared event source stream. The route still works, but Arc appends by those strings rather than through the definition: `EventContext.EventSource` and the selected definition's concurrency dimensions are lost.

When the strings equal the enclosing command's `[EventSource<T>(stream)]` route, the message also says the override is redundant. A different declared route remains supported; this suggestion is about retaining the definition, not making every event follow the command.

## Severity

Info

## Example

This wrapper names the declared `Account.Transactions` stream using legacy strings:

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventSource]
[EventStream("Transactions")]
public class AccountEventSource : IEventSource;

public static class AccountEvents
{
    public static EventForEventSourceId Route(EventSourceId id, object fact) =>
        new(id, fact)
        {
            EventSourceType = "Account",
            EventStreamType = "Transactions" // ARCCHR0021
        };
}
```

Replace the initializer's routing assignments with:

```csharp
EventSource = typeof(AccountEventSource),
EventStream = "Transactions"
```

This initializer excerpt uses the definition above. It preserves the source and stream names while retaining the definition for the append.

## Quick fix

**Use event source definition routing** replaces the legacy assignments with `EventSource = typeof(Definition)` and `EventStream = "stream"`. It keeps unrelated wrapper values, including stream id, subject, occurrence time, and tags. Fix All is supported.

Review concurrency before applying the fix: the definition's policy now participates in ordinary derived guards. Explicit command `concurrency: true` choices still win, and returned exact scopes still replace derived scopes. The fix does not remove command attributes or rewrite legacy routes automatically at runtime.

## Boundaries

The analyzer recognizes literal strings, including routing-concept constructors around literals, on object-creation and `with` initializers. Legacy source-name lookup covers definitions in the same compilation only; ambiguous names, unknown names, undeclared streams, computed values, sentinels, and unknown inherited routing are left alone. If only a stream is set, the enclosing `[Command]`'s `[EventSource<T>]` supplies the source. Typed routing and `IEventRoutes` / `route.Route(...)` need no migration diagnostic.

## See also

- [ARCCHR0020: Wrapper event stream is not declared](./ARCCHR0020.md)
- [Per-event routing](../commands/events.md#per-event-routing)
- [Concurrency](../commands/concurrency.md)
- [Route a command through an event source definition](../commands/event-source-definitions.md#wrapper-named-streams): the migration this rule suggests.
- [Chronicle's Dynamic Consistency Boundary](/chronicle/dynamic-consistency-boundary/): the boundary the definition's concurrency dimensions give an append.
