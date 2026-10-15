---
title: Test the race
description: Test that a rule holds when two writers race, with a command scenario that queues a competing append after the command has read its state.
---

Use this page when you want a test that proves a rule holds under concurrency: the command reads, a competitor writes, and the command must not commit.

| You want to test | Use |
| --- | --- |
| A guarded read changed before commit | `new CommandScenario<T>()`, `Given.ForEventSource(...)`, `AppendConcurrently(...)` |
| An explicit scope or a stream decision | The same, with a route for a stream |
| A validator's rule | The same scenario. The validator reads the same log |
| A plain `Off` command without protected dependencies | `new CommandScenario<T>()`. Pinned read models stand in for reads |

The scenario automatically selects one real in-process event log for commands with read protection `On` or `Measure`, protected decisions, decision dependencies, per-stream read models or guarded generated parameters. Seeded events, guarded reads and commits see the same history. Pinned read models cannot supply a guard, so seed real events instead.

## A race on one entity

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Concepts;
using FluentValidation;
using Xunit;

public record Isbn(string Value) : EventSourceId(Value);

public record MemberId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
public record BookAdded();

[EventType]
public record BookBorrowed(MemberId Member);

public record BookStock(
    EventSourceId Id,
    [Increment<BookAdded>, Decrement<BookBorrowed>] int Available);

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
            .WithMessage("No copies left.");
}

public class when_borrowing_the_last_copy_while_another_member_borrows_it
{
    [Fact]
    public async Task should_reject_the_command_with_a_concurrency_violation()
    {
        await using var scenario = new CommandScenario<BorrowBook>();
        var isbn = new Isbn("978-0-00-000001-0");
        scenario.Given.ForEventSource(isbn).Events(new BookAdded());
        scenario.AppendConcurrently(isbn, new BookBorrowed(new MemberId(Guid.NewGuid())));

        var result = await scenario.Execute(new BorrowBook(isbn, new MemberId(Guid.NewGuid())));

        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        Assert.Empty(scenario.AppendedEvents);
    }
}
```

`AppendConcurrently` appends after the command read its state and before it commits. The command's own event is not written, so the scenario records no appended events for it.

The read protection mode comes from the assembly that declares the command, or from `[ReadProtection(...)]` on it, as in production. Test the command in the mode it runs in.

## Other checks

| Check | How |
| --- | --- |
| That a command with `Off` still behaves | Run the scenario in a project that is `Off`. It exercises plain reads |
| That a stream decision guards a stream | Seed and compete on the same route. See [Stream decisions](../../commands/stream-decisions.md#seed-and-interfere-on-the-same-route) |
| That Chronicle admits a read model before production | [Check that Chronicle admits the decision reads](../../../testing/chronicle.md#check-that-chronicle-admits-the-decision-reads) |

For the rest of the scenario API, see [Testing Chronicle commands](../../../testing/chronicle.md).

## Related

- [Getting started](../getting-started.md)
- [Chronicle: test the race](/chronicle/dynamic-consistency-boundary/scenarios/)
