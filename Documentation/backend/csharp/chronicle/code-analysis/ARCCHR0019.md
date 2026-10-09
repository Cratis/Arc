---
title: 'ARCCHR0019: Event stream id template is only resolved for commands'
description: A reactor has an [EventStreamId] value with {Property} placeholders, but only Arc commands resolve them.
---

## Rule

Chronicle reactors read `[EventStreamId]` as a literal constant. Only Arc commands resolve `{Property}` placeholders against their properties. A reactor class (a type implementing `IReactor`) whose `[EventStreamId]` value contains a placeholder therefore appends its side-effect events to a stream literally named, for example, `{Period}`.

A constant value is not reported, nor is an escaped `{{`/`}}` pair. Types that are neither commands nor reactors are not inspected.

This is a separate rule from [ARCCHR0017](./ARCCHR0017.md): ARCCHR0017 is an error about a template on a command that cannot be resolved, while this is a warning about a template that will not be resolved at all.

## Severity

Warning

## Example

```csharp
[EventStreamId("{Period}")] // ARCCHR0019: used literally by the reactor
public class PeriodReactor : IReactor
{
    ...
}
```

Use a constant stream id, or route the events explicitly.

## See also

- [Concurrency](../commands/concurrency.md)
