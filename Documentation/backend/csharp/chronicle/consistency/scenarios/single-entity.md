---
title: Cratis Arc consistency for one entity
description: Protect a book's last copy in Cratis Arc with a read-model validator, and distinguish a business-rule rejection from a concurrent change.
---

Use a protected read-model parameter in Cratis Arc when a command depends on one entity's state. For a book loan, the validator checks that a copy is available; read protection prevents two commands from both borrowing the last copy after reading the same stock.

Start with [read protection enabled](../getting-started.md#1-turn-read-protection-on) in the assembly containing this command. The example assumes `On` at assembly level. Under `Off`, the validator can reject an unavailable book, but its read does not protect an available copy from a competing borrower.

| | |
| --- | --- |
| Rule | A book cannot be borrowed when no copy is left |
| Mechanism | A read model parameter under `On`, the rule in a validator |
| Alternatives | An aggregate (more code, no read model). `IDecisionReads` directly in `Handle()` for a computed key, with a reasoned ARCCHR0040 suppression linked to the tracking feature |
| Not | A validator over a plain read under `Off`: it checks the value but does not guard against a competing append |

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

`BookStock` folds the book's own events into `Available`. The nullable validator parameter lets a book without history reach the rule as `null`, so it is rejected with `No copies left.` just like a book with no available copies. With stock available, `Handle()` returns `BookBorrowed` for that book.

The guard records which events could change the stock and the position captured before the read:

```text
guard isbn: types=[BookAdded, BookBorrowed, BookReturned], after <tail before the read>
```

## What holds

Check the outcomes separately:

- With one copy available and no competing stock change, borrowing succeeds and appends one `BookBorrowed` event.
- With zero copies or no book history, validation returns `No copies left.` and appends nothing.
- If two commands read the last copy before either commits, only one can append. The loser gets a `400` with the reason `concurrencyViolation`. For a book key `isbn` and a conflicting event at position `1042`, the message is `BookStock 'isbn' changed after BorrowBook read it (BookBorrowed at 1042). Nothing from this command was written; submit it again.`
- Only the model's event types conflict. A `BookRenamed` for the same book does not.

## What does not hold

- The guard covers one key. A rule over two known books needs a protected read for each. If the set of books can grow, put the facts needed for the limit on one summary entity. See [two entities](./two-entities.md).
- A rejected command writes nothing. After a concurrency conflict, the client must read again before offering resubmission. Arc never resubmits.

To specify the available and unavailable cases without simulating competing writers, follow [Specify a state-dependent rule](./testing.md).

## Related

- [Validators](../validators.md)
- [Getting started](../getting-started.md)
- [Chronicle: decision reads](/chronicle/read-models/decision-reads/)
