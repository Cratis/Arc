---
title: "ARCCHR0002: Ambiguous command identity"
description: A command has several properties that can be its event source id, and nothing says which one to use.
---

## Rule

**Warning**. A `[Command]` has two or more public instance properties that can supply the command's event source id, does not implement `ICanProvideEventSourceId`, and does not return events that carry their own event source id.

A property is a candidate when it is any of these:

| Candidate | Example |
| --- | --- |
| A type that is or derives from `EventSourceId` | `Isbn Isbn` where `Isbn : EventSourceId` |
| A type with an implicit conversion to `EventSourceId` | A `ConceptAs<Guid>` with an implicit conversion |
| A property marked with Chronicle's `[Key]` | `[Key] Guid OrderId` |
| A positional parameter marked with `[Key]` and the property it creates | Counted once |

Without a decision, Arc uses the first matching property. That choice also decides which entity the command appends to and which key a read model parameter reads by default, so a silent wrong choice changes the rule.

## Resolve the diagnostic

| Situation | Fix |
| --- | --- |
| Only one of the properties is the command's subject | Make the others `ConceptAs<T>` values, not `EventSourceId` |
| The subject is computed or fixed | Implement `ICanProvideEventSourceId` and return the id |
| The command appends to both entities | Return `EventForEventSourceId` for each event. Each event then carries its own id and the rule does not apply |

The analyzer also recognizes a single wrapper or a sequence of wrappers in the success branch of `Result<TEvent, TError>`, including `Task<Result<TEvent, TError>>`. No `ICanProvideEventSourceId` is needed for those return values. An error branch carrying a wrapper does not give an untargeted success event a destination.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

public record Isbn(string Value) : EventSourceId(Value);

public record MemberId(string Value) : EventSourceId(Value);

[EventType]
public record BookBorrowed(MemberId Member);

[Command]
public record BorrowBook(Isbn Isbn, MemberId Member) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => Isbn;

    public BookBorrowed Handle() => new(Member);
}
```

`[Key]` on one of the properties does not resolve the ambiguity. It marks a candidate. See [Resolving EventSourceId](../resolving-event-source-id.md) for the rules Arc applies at run time.

## Boundaries

This analyzer's candidate rule includes implicit conversions, which run-time discovery does not generally do. A conversion-only `ConceptAs<Guid>` is not a safe run-time identity declaration. Use `EventSourceId<Guid>` ancestry or a selected key.

Neither a clean analyzer result nor a recognized return type proves that dependencies resolved before `Handle()` (an aggregate, a read model parameter) use the id you intend. Under read protection `On`, name the key of a read model parameter with `[LookUpBy]`. No code fix is offered.
