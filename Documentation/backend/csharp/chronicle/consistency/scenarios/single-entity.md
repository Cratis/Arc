---
title: One entity
description: A rule over the state of one entity, such as stock or a count, written as a read model parameter and a validator.
---

Use this when a command may proceed only if the stored state of one entity allows it: the last copy of a book, a limit on one member, a balance.

| | |
| --- | --- |
| Rule | A book cannot be borrowed when no copy is left |
| Mechanism | A read model parameter under `On`, the rule in a validator |
| Alternatives | An aggregate (more code, no read model). `IDecisionReads` in `Provide()` for a computed key |
| Not | A validator over a plain read under `Off`. It is only a hint |

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Concepts;
using FluentValidation;

public record Isbn(string Value) : EventSourceId(Value);

public record MemberId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
public record BookAdded();

[EventType]
public record BookBorrowed(MemberId Member);

[EventType]
public record BookReturned(MemberId Member);

public record BookStock(
    EventSourceId Id,
    [Increment<BookAdded>, Decrement<BookBorrowed>, Increment<BookReturned>] int Available);

[Command]
public record BorrowBook(Isbn Isbn, MemberId Member)
{
    public BookBorrowed Handle() => new(Member);
}

public class BorrowBookValidator : CommandValidator<BorrowBook>
{
    public BorrowBookValidator(BookStock? stock) =>
        RuleFor(command => command.Isbn)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies left.");
}
```

```text
guard isbn: types=[BookAdded, BookBorrowed, BookReturned], after <tail before the read>
```

## What holds

- Two members who borrow the last copy at the same time cannot both succeed. The second gets a `400` with the reason `concurrencyViolation` and the message `BookStock 'isbn' changed after BorrowBook read it. Nothing from this command was written; submit it again.`
- Only the model's event types conflict. A `BookRenamed` for the same book does not.
- A book that has no events yet reads as `null`, and the rule above rejects it.

## What does not hold

- The guard covers one key. A rule over several books needs a model that folds all of them, or two reads. See [two entities](./two-entities.md).
- A rejected command writes nothing, so the client must read again before it tries again. Arc never resubmits.

## Related

- [Validators](../validators.md)
- [Getting started](../getting-started.md)
- [Chronicle: decision reads](/chronicle/read-models/decision-reads/)
