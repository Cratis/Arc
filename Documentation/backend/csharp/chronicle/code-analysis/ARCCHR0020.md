---
title: 'ARCCHR0020: Wrapper event stream is not declared'
description: A literal per-event route names a stream its known event source definition does not declare.
---

## Rule

A literal route on `EventForEventSourceId` names a stream that its event source definition does not declare with `[EventStream]`. This is a routing typo, not a warning about choosing a route different from the command's.

The analyzer checks object-creation and `with` initializers. It resolves `EventSource = typeof(Definition)` directly, including definitions in referenced assemblies. For legacy `EventSourceType` strings, it looks for an unambiguously named definition in the current compilation. An unset source uses the enclosing `[Command]`'s `[EventSource<T>]` definition. The stream must be named on the wrapper itself, not inherited from the command. A source-only legacy override does not trigger this warning. Outside a command, both the source and stream must be known.

## Severity

Warning

## Example

This example declares `Transactions`, but routes the returned event to `Transactons`:

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
            EventSource = typeof(AccountEventSource),
            EventStream = "Transactons" // ARCCHR0020
        };
}
```

Use `EventStream = "Transactions"`, or declare the intended alternative stream on the definition. The same warning applies to `EventSourceType = "Account", EventStreamType = "Transactons"`. There is no automatic fix: the analyzer cannot decide whether you meant an existing stream or a new declaration.

## Boundaries and runtime behavior

- Typed alternative declared streams are valid, even when they differ from the command route.
- Computed strings, `route.Route(...)`, unknown source names, and unset `EventSourceType.Default` / `EventStreamType.All` values are not reported. Literal `"Default"` and `"All"` have the same sentinel behavior on those legacy properties.
- Stream ids are free-form and are not checked against definitions.
- The analyzer does not infer routing inherited from an unknown `with` receiver.
- For definition-routed commands, a legacy wrapper route whose resolved source matches a registered definition but whose wrapper-named `EventStreamType` is undeclared throws `EventStreamDoesNotBelongToEventSource` before any returned events are enrolled or appended. This includes computed values and exact-scope responses. A stream-only override is checked against the command's definition; a source-only override inherits the command's stream string without this check. Unknown source names and commands without a definition keep their legacy behavior.
- Framework dependency injection uses the validating handler constructors taking `IEventSources`. The obsolete legacy constructors remain available for manual construction but do not perform this registry-based validation.

## See also

- [ARCCHR0021: Use event source definition routing](./ARCCHR0021.md)
- [Per-event routing](../commands/events.md#per-event-routing)
