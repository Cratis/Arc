---
title: 'ARCCHR0018: Command declares its event stream id twice'
description: A command has [EventStreamId] with a value and also implements ICanProvideEventStreamId.
---

## Rule

A command can declare its event stream id with `[EventStreamId(value)]` (a constant or a `{Property}` template) or with `ICanProvideEventStreamId`. Doing both is ambiguous, and Arc rejects the command at runtime with `AmbiguousEventStreamId`. This rule reports it at build time.

`[EventStreamId]` without a value (or with `null`) does not declare an id and is not reported. This rule is separate from [ARCCHR0002](./index.md#arcchr0002-ambiguous-command-identity), which is about the event source id and does not look at stream ids.

## Severity

Error

## Example

```csharp
[Command]
[EventStreamId("{Period}")] // ARCCHR0018
public record CloseBooks(string Period) : ICanProvideEventStreamId
{
    public EventStreamId GetEventStreamId() => Period;
}
```

Remove the value from the attribute or stop implementing the interface. Use the attribute for a constant or template, and the interface for an id that needs code.

## See also

- [ARCCHR0017](./ARCCHR0017.md)
- [Concurrency](../commands/concurrency.md)
