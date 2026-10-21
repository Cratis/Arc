---
title: Getting started with read protection
description: Turn read protection On, write a first protected command with a validator over a read model, and see what happens when two requests race.
---

Use this page when you want one command whose rule holds under concurrent requests. It takes about ten minutes. You need an Arc application with Chronicle (see [Add event sourcing to an Arc slice](../add-event-sourcing.mdx)) and a Chronicle version that includes read protection (19.68.0 or later).

## 1. Turn read protection On

Set the mode in the project that contains your commands, or in a `Directory.Build.props` that covers several projects.

```xml
<PropertyGroup>
    <CratisReadProtection>On</CratisReadProtection>
</PropertyGroup>
```

The value must be `Off`, `Measure` or `On`. Any other value fails the build with `CratisReadProtection must be Off, Measure, or On.` The property is compiled into the assembly. It is not read from configuration, so analyzers and tests see the same mode that runs in production.

To turn it on for one command instead, put `[ReadProtection(ReadProtectionMode.On)]` (namespace `Cratis.Arc.Commands`) on the command. A command-level value overrides the assembly's value.

## 2. Write a protected command

The rule: a book cannot be borrowed when no copy is left. The rule goes in a `CommandValidator<T>` that takes the read model as a constructor parameter. `Handle()` says what happens. The command-level declaration keeps this example protected when copied into a project that has not enabled `On` assembly-wide.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Concepts;
using FluentValidation;

public record Isbn(string Value) : EventSourceId(Value);

public record MemberId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
public record CopyAdded();

[EventType]
public record BookBorrowed(MemberId Member);

[EventType]
public record BookReturned(MemberId Member);

[ReadModel]
public record BookStock(
    Isbn Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>, Increment<BookReturned>] int Available);

[Command, ReadProtection(ReadProtectionMode.On)]
public record BorrowBook(Isbn Isbn, MemberId Member)
{
    public BookBorrowed Handle() => new(Member);
}

public class BorrowBookValidator : CommandValidator<BorrowBook>
{
    public BorrowBookValidator(BookStock? stock) =>
        RuleFor(command => command.Isbn)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies of this book are available.");
}
```

What makes this work:

- `Isbn` derives from `EventSourceId`, so Arc uses it as the command's event source id. No `[Key]` is needed. It is also the key of the read.
- `MemberId` is a `ConceptAs<Guid>`, not an `EventSourceId`: it refers to another entity and this command does not append to it. A command with two `EventSourceId` properties is ambiguous unless `Handle()` returns `EventForEventSourceId` or the command implements `ICanProvideEventSourceId`; see [ARCCHR0002](../code-analysis/ARCCHR0002.md).
- `BookStock` is a model-bound projection of the book's own events. It has an `Id` property of a string-like or `Guid` type. The key is matched to it.
- `BookStock?` is nullable because a book with no events has no read model yet. A non-nullable parameter fails the command with `ReadModelDoesNotExistForCommand` instead.
- The rule is in the validator because that is where a "may this command proceed" rule belongs. Under `On` the validator's read-model parameter is guarded. See [Validators](./validators.md).

## 3. What happens when two requests race

1. Arc starts the command's unit of work and turns on read protection for the whole invocation.
2. Before the validator runs, Chronicle captures the position of the event log, then folds `BookStock` from the book's own events.
3. The validator decides from that instance. `Handle()` returns `BookBorrowed`.
4. Arc commits. Chronicle appends the event and rejects the append if a `CopyAdded`, `BookBorrowed` or `BookReturned` for that book arrived after the position it captured.
5. On a conflict nothing is written. The caller receives a `400` with this message:

```text
BookStock 'isbn-1' changed after BorrowBook read it (BookBorrowed at 1042). Nothing from this command was written; submit it again.
```

The validation result has the reason `concurrencyViolation` and the detail `ReadChanged`. Arc and Chronicle do not re-run the command; the fold may retry up to three times. The client keeps the user's input, reads again and offers resubmission.

The guard for this command is:

```text
guard isbn-1: types=[CopyAdded, BookBorrowed, BookReturned], after <position before the read>
```

A concurrent event of another type on the same book, for example a rename, does not conflict. A borrow of another book does not conflict.

## 4. See a refusal

Under `On`, a read Arc cannot guard is refused at once, instead of running unguarded. Add a plain `IReadModels` read to `Handle()`:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

[Command]
public record LendBook(Isbn Isbn, MemberId Member)
{
    public async Task<BookBorrowed> Handle(IReadModels readModels)
    {
        var stock = await readModels.GetInstanceById<BookStock>(Isbn);
        return new(Member);
    }
}
```

The command fails with a `500`:

```text
LendBook called GetInstanceById while read protection is On. Take a read model as a parameter, use IDecisionReads.Get in Provide, or mark the method [Unprotected("why")].
```

The fix is the parameter from step 2. If the read is only context, mark the method or the read `[Unprotected("why")]`. [Refusals](./refusals.md) lists every refusal with its fix.

## 5. Test the race

A scenario test appends a competing event after the command read and before it committed. See [Test the race](./scenarios/testing.md).

## Where to go next

- [Adopting read protection](./adopting.md): turn it on in an existing application without breaking it.
- [Reads](./reads.md): what is protected, advisory and refused.
- [Scenarios](./scenarios/index.md): the other rule shapes.
