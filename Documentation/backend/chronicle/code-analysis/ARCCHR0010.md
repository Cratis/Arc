---
title: 'ARCCHR0010: Raw Guid response does not set the event source id'
description: A heuristic checks whether a keyless command's ordinary Guid response was intended to identify its event source.
---

## Rule

A raw `Guid` in a command response tuple is an ordinary response value. It is not Chronicle event-source metadata.

This rule reports a keyless `[Command]` whose `Handle()` method returns both:

- a raw `Guid`; and
- a statically identifiable untargeted `[EventType]` event (including supported typed event collections).

This rule belongs to the optional Arc–Chronicle analyzer package. It does not restrict ordinary standalone Arc command responses.

Chronicle resolves a keyless command's fallback event source id before `Handle()` runs. If `Handle()` later returns `(Guid, event)`, the caller receives that `Guid`, but the event is appended under the earlier fallback id. Return `EventSourceId<Guid>` or, preferably, a domain identity derived from it when the response is meant to identify the new event source.

The rule stays silent when runtime-recognized command properties or `ICanProvideEventSourceId` supply an identity, or the tuple includes explicit event-source identity metadata. `EventForEventSourceId` targets its own event; a sibling bare event can still trigger the rule.

An unrelated confirmation Guid is also valid on a **keyless** command using a generated fallback stream. This is a signature heuristic, not proof of a defect or a runtime restriction. Check intent before changing routing.

## Severity

Warning

## Example

### Violation

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[Command]
public record SubmitExpense(decimal Amount)
{
    // ARCCHR0010: this Guid is only the response; it does not select the event source
    public (Guid, ExpenseSubmitted) Handle()
    {
        var expenseId = Guid.NewGuid();

        return (expenseId, new ExpenseSubmitted(Amount));
    }
}

[EventType]
public record ExpenseSubmitted(decimal Amount);
```

### Fix with a domain identity

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

public record ExpenseId(Guid Value) : EventSourceId<Guid>(Value)
{
    public static ExpenseId New() => new(Guid.NewGuid());
}

[Command]
public record SubmitExpense(decimal Amount)
{
    public (ExpenseId, ExpenseSubmitted) Handle()
    {
        var expenseId = ExpenseId.New();

        return (expenseId, new ExpenseSubmitted(Amount));
    }
}

[EventType]
public record ExpenseSubmitted(decimal Amount);
```

The returned `ExpenseId`, the appended event's `EventSourceId`, and the projected read-model key now identify the same expense.

## Quick Fix

**Use `EventSourceId<Guid>` for the response identity** changes exactly one raw Guid element in supported direct tuple return syntax. Eligible handlers are declared on the command itself, non-virtual/non-override/non-abstract, and return a direct tuple or an async `Task`/`ValueTask` of a tuple. The current project must compile before and after the edit.

There is no Fix All. No fix is offered for aliases, inherited handlers, unsupported factory/Result/OneOf returns, multiple Guid elements, or a change that produces compilation errors. Analyzer coverage is broader than fix coverage. The fix does not rewrite arbitrary bodies or dependent projects, and compilation does not prove domain intent.

Prefer a domain identity such as `ExpenseId : EventSourceId<Guid>` when appropriate; the tool cannot infer its name. **This edit changes persistence routing**, not just serialization or style.

## Intentional ordinary Guid response

Do not invent a separate input identity solely to satisfy the analyzer. If fallback routing and an unrelated receipt are intentional, keep runtime behavior and suppress narrowly. This command fragment uses an application `ExpenseSubmitted` event:

```csharp
[Command]
public record SubmitExpense(decimal Amount)
{
#pragma warning disable ARCCHR0010 // Receipt id is unrelated to the generated event source.
    public (Guid ReceiptId, ExpenseSubmitted Event) Handle() =>
        (Guid.NewGuid(), new ExpenseSubmitted(Amount));
#pragma warning restore ARCCHR0010
}
```

For inherited handlers the diagnostic is on the command declaration, so place the narrow suppression there. Keep a regression spec asserting the response and event-source identities intentionally differ.

## Analysis boundaries

The current implementation examines public instance handlers, including inherited ones; known `Task<T>`, `ValueTask<T>`, `Result<,>`, and `OneOf` branches; and reference-type event arrays/typed collections. It uses runtime identity eligibility separately from ARCCHR0002's broader implicit-conversion convention.

It does not infer erased `object`/`IEnumerable<object>` contents or arbitrary factory bodies. Custom lookalike wrappers and arbitrary `IOneOf` implementations are not treated as known unions. A clean result is not proof that every runtime event has the intended target.

## Why This Rule Exists

Both values look like the identity of the created entity, and the command still succeeds. The mismatch only becomes visible when the caller uses the response to query the new entity or execute a follow-up command: Chronicle persisted the event under another id, so the lookup finds nothing.

This shape appeared in an earlier starter template and in the generic tuple documentation. It compiles because `(Guid, event)` is a valid response-plus-side-effect tuple; it is a defect only when the ordinary Guid was intended to identify the created stream.

## Related Rules

- [ARCCHR0002](index.md#rules-overview) — ambiguous command event source id
- [ARCCHR0008](ARCCHR0008.md) — command key marked with the data annotations Key attribute

## See also

- [Returning EventSourceId from a command](../commands/returning-event-source-id.md)
- [Resolving EventSourceId](../resolving-event-source-id.md)
- [Command response value handlers](../../commands/response-value-handlers.md)
