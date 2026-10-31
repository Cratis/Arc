---
title: Two entities
description: A rule over two entities at once, written as two read model parameters in a validator and one batch with one event for each entity.
---

Use this when one rule depends on the state of two entities: a member's loan limit and a book's last copy, or a course's capacity and a student's limit.

| | |
| --- | --- |
| Rule | A member may hold three loans, and a book cannot be borrowed when no copy is left |
| Mechanism | Two validator constructor parameters, each with `[LookUpBy]`, and one event for each entity in one atomic batch |
| Alternatives | Plain `Handle` parameters, then `[LookUpBy]` for command keys, then per-stream models for the command route. Only keys no parameter can express use `IDecisionReads` in `Provide()`, returning plain state. `Handle()` can return a `Result` for a rule computed there |
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

The validator reads and checks both entities. `Handle()` says only what happens. Both guards are checked in the one atomic append. If either side changed, nothing is written and the message names the model that changed.

Things to get right:

- Each `EventForEventSourceId` carries its own destination. No `ICanProvideEventSourceId` is needed, including when `Handle()` returns these values inside a `Result` or a `Task` ([ARCCHR0002](../../code-analysis/ARCCHR0002.md)).
- Each entity gets an event of its own type. `BookBorrowed` goes to the book and `LoanTaken` goes to the member. Do not write the same event type to both: each model would fold the other entity's copy.
- The model of each entity must be keyed by that entity and folded from events appended to it. `MemberLoans` counts `LoanTaken` on the member's own stream.
- `[LookUpBy]` names a property of the command, even on a validator constructor. Arc converts its value to the wire key: an `Isbn` can select a model with an `EventSourceId Id`. The CLR types need not be identical.

### The trap: a model keyed by an event property

It is tempting to build `MemberLoans` from the book's events, keyed by the member named inside the event (`UsingKey(e => e.Member)`). Chronicle refuses that model, because a conflict on the member cannot be detected from the book's stream. The command fails with:

```text
BorrowBook reads 'loans' (MemberLoans) keyed by an event property, so read protection cannot guard it. If it is only context, mark it [Unprotected("why")]. Otherwise append a fact to the entity it is keyed by, or use a constraint.
```

The fix is the example above: append `LoanTaken` to the member.

## A course and a student

Course capacity and a per-student limit use the same pattern.

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

A decision on a key guards that key even when the command writes only to another one. A competing subscription for the student conflicts with this command.

Chronicle writes one event for each entity. A model of the Dynamic Consistency Boundary would write one event that carries both tags; Chronicle has no tags in a guard, so the fact is two events in one atomic append. See [Chronicle: when one fact becomes two events](/chronicle/dynamic-consistency-boundary/chronicle/#when-one-fact-becomes-two-events).

## What does not hold

This guards exactly the keys you read. If the set of keys can grow, for example a limit across loans that each live on their own book stream, a new key enters nobody's guard. Model such a limit on one key that carries all the facts, as `MemberLoans` does.

## Related

- [Reads](../reads.md#choose-the-key-with-lookupby)
- [Chronicle: cross-entity rules](/chronicle/dynamic-consistency-boundary/scenarios/)
