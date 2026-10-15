---
title: "ARCCHR0014: Observed event stream is not declared"
description: A reactor or reducer is filtered to a stream its event source definition does not declare, so it would never see an event.
---

## Rule

**Warning**. A reactor or reducer carries `[FromEventSource<TSource>("stream")]`, and the definition `TSource` does not declare a stream named `stream` with `[EventStream("stream")]`.

A stream that the definition does not declare cannot match, so no event would reach the observer. The diagnostic is reported on the attribute.

## Resolve the diagnostic

Declare the stream on the definition, or observe a stream it declares.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Reactors;

[EventSource("Account")]
[EventStream("Transactions")]
public class AccountEventSource : IEventSource;

[EventType]
public record TransactionRecorded(decimal Amount);

[FromEventSource<AccountEventSource>("Transactions")]
public class TransactionObserver : IReactor
{
    public Task Recorded(TransactionRecorded @event) => Task.CompletedTask;
}
```

Misspelling `"Transactions"` in the attribute, or removing the `[EventStream]` attribute from the definition, reports this rule.

## Boundaries

Only the definition's own attributes are read, so a definition in a referenced assembly is checked the same way as one in your project. A stream argument that is not a constant string, and a definition that is not resolved yet, are left alone. No code fix is offered.
