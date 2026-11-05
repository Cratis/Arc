---
title: Cratis Arc consistency across two entities
description: Check book stock and a member's loan limit in Cratis Arc, then append both events atomically or reject the command without writing either.
---

Protect both read-model parameters in Cratis Arc when a command depends on two entities. Borrowing a book must leave both the book's stock and the member's loan count valid. Read both before deciding, then append an event to each entity in one atomic batch.

Enable [read protection at assembly level](../getting-started.md#1-turn-read-protection-on) before using these examples. Both commands assume `On`; their validators alone do not protect the decision from concurrent changes.

| | |
| --- | --- |
| Rule | A member may hold three loans, and a book cannot be borrowed when no copy is left |
| Mechanism | Two validator constructor parameters, each with `[LookUpBy]`, and one event for each entity in one atomic batch |
| Alternatives | Plain `Handle` parameters, then `[LookUpBy]` for command keys, then per-stream models for the command route. Only keys no parameter can express use `IDecisionReads` directly in `Handle()`, with a reasoned ARCCHR0040 suppression linked to the tracking feature. `Handle()` can return a `Result` for a rule computed there |
| Not | Two aggregates, each guarding only its own source. One model that folds both entities: it is refused |

## A member and a book

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

public record Isbn(string Value) : EventSourceId(Value);

public record MemberId(string Value) : EventSourceId(Value);

[EventType]
public record BookAdded();

[EventType]
public record BookBorrowed(MemberId Member);

[EventType]
public record LoanTaken(Isbn Book);

[EventType]
public record LoanReturned(Isbn Book);

public record BookStock(
    EventSourceId Id,
    [Increment<BookAdded>, Decrement<BookBorrowed>] int Available);

public record MemberLoans(
    EventSourceId Id,
    [Increment<LoanTaken>, Decrement<LoanReturned>] int Active);

[Command]
public record BorrowBook(Isbn Isbn, MemberId Member)
{
    public IEnumerable<EventForEventSourceId> Handle() =>
    [
        new(Isbn, new BookBorrowed(Member)),
        new(Member, new LoanTaken(Isbn))
    ];
}

public class BorrowBookValidator : CommandValidator<BorrowBook>
{
    public BorrowBookValidator(
        [LookUpBy(nameof(BorrowBook.Isbn))] BookStock? stock,
        [LookUpBy(nameof(BorrowBook.Member))] MemberLoans? loans)
    {
        RuleFor(command => command.Isbn)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies left.");
        RuleFor(command => command.Member)
            .Must(_ => loans is null || loans.Active < 3)
            .WithMessage("The member has reached the loan limit.");
    }
}
```

```text
guard isbn:   types=[BookAdded, BookBorrowed], after <tail before the BookStock read>
guard member: types=[LoanTaken, LoanReturned], after <tail before the MemberLoans read>
```

`[LookUpBy]` selects the book and member from the command's properties. The validator checks the stock and loan count before `Handle()` returns the two events. A member with no loan history has a count of zero for this rule; a book with no stock history is unavailable.

With a copy available and fewer than three active loans, the command appends `BookBorrowed` to the book and `LoanTaken` to the member. If either rule fails, it appends neither. If a matching event changes either entity after its read, the atomic append rejects both events and the concurrency message names the model that changed.

Keep the event destinations aligned with the reads:

- Each `EventForEventSourceId` carries its own destination. No `ICanProvideEventSourceId` is needed, including when `Handle()` returns these values inside a `Result` or a `Task` ([ARCCHR0002](../../code-analysis/ARCCHR0002.md)).
- Each entity gets an event of its own type. `BookBorrowed` goes to the book and `LoanTaken` goes to the member. Do not write the same event type to both: each model would fold the other entity's copy.
- The model of each entity must be keyed by that entity and folded from events appended to it. `MemberLoans` counts `LoanTaken` on the member's own stream.
- `[LookUpBy]` names a property of the command, even on a validator constructor. Arc converts its value to the wire key: an `Isbn` can select a model with an `EventSourceId Id`. The CLR types need not be identical.

### The trap: a model keyed by an event property

It is tempting to build `MemberLoans` from the book's events, keyed by the member named inside the event (`UsingKey(e => e.Member)`). Chronicle refuses that model, because a conflict on the member cannot be detected from the book's stream. The command fails with:

```text
BorrowBook reads 'loans' (MemberLoans) keyed by an event property, so read protection cannot guard it. If it is only context, mark it [Unprotected("why")]. Otherwise append a fact to the entity it is keyed by, or use a constraint.
```

Append `LoanTaken` to the member, as in the example above, so the member's history contains every fact needed for its loan count. Adding another read cannot make a model keyed by an event property protectable.

## A course and a student

Use the same pattern when a course has limited seats and each student may join at most three courses. The course needs an existing capacity record; a student without course history starts with a count of zero.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

public record CourseId(string Value) : EventSourceId(Value);

public record StudentId(string Value) : EventSourceId(Value);

[EventType]
public record CourseDefined(int Capacity);

[EventType]
public record SeatTaken(StudentId Student);

[EventType]
public record CourseJoined(CourseId Course);

[FromEvent<CourseDefined>]
public record CourseSeats(
    EventSourceId Id,
    int Capacity,
    [Increment<SeatTaken>] int Taken);

public record StudentCourses(
    EventSourceId Id,
    [Increment<CourseJoined>] int Count);

[Command]
public record SubscribeToCourse(CourseId Course, StudentId Student)
{
    public IEnumerable<EventForEventSourceId> Handle() =>
    [
        new(Course, new SeatTaken(Student)),
        new(Student, new CourseJoined(Course))
    ];
}

public class SubscribeToCourseValidator : CommandValidator<SubscribeToCourse>
{
    public SubscribeToCourseValidator(
        [LookUpBy(nameof(SubscribeToCourse.Course))] CourseSeats? course,
        [LookUpBy(nameof(SubscribeToCourse.Student))] StudentCourses? student)
    {
        RuleFor(command => command.Course)
            .Must(_ => course is not null)
            .WithMessage("The course does not exist.")
            .Must(_ => course is null || course.Taken < course.Capacity)
            .WithMessage("The course is full.");
        RuleFor(command => command.Student)
            .Must(_ => student is null || student.Count < 3)
            .WithMessage("The student is already in three courses.");
    }
}
```

```text
guard course:  types=[CourseDefined, SeatTaken], after <tail before the CourseSeats read>
guard student: types=[CourseJoined], after <tail before the StudentCourses read>
```

With a free seat and a student below the limit, the command appends one `SeatTaken` to the course and one `CourseJoined` to the student. A missing or full course, or a student already in three courses, produces the corresponding validation message and writes neither event. A competing subscription that changes either protected model before commit also prevents both appends.

A command's protected read guards the key it reads even when that command appends only to a different entity. The keys read, not just the append destinations, determine what can conflict.

Chronicle writes one event for each entity. A model of the Dynamic Consistency Boundary would write one event that carries both tags; Chronicle has no tags in a guard, so the fact is two events in one atomic append. See [Chronicle: when one fact becomes two events](/chronicle/dynamic-consistency-boundary/chronicle/#when-one-fact-becomes-two-events).

## What does not hold

This guards exactly the keys you read. If the set of keys can grow, for example a limit across loans that each live on their own book stream, a new key enters nobody's guard. Model such a limit on one key that carries all the facts, as `MemberLoans` does.

Use [state-dependent command specs](./testing.md) to check the accepted case and each rule's rejection separately, including that a rejection appends no events.

## Related

- [Reads](../reads.md#choose-the-key-with-lookupby)
- [Chronicle: cross-entity rules](/chronicle/dynamic-consistency-boundary/scenarios/)
