---
title: Two entities
description: A rule over two entities at once, written as two read model parameters and one batch with one event for each entity.
---

Use this when one rule depends on the state of two entities: a member's loan limit and a book's last copy, or a course's capacity and a student's limit.

| | |
| --- | --- |
| Rule | A member may hold three loans, and a book cannot be borrowed when no copy is left |
| Mechanism | Two read model parameters, each with `[LookUpBy]`, and one event for each entity in one atomic batch. The rule is in `Handle()`, which returns a `Result` |
| Alternatives | `IDecisionReads` in `Provide()` when the second key is computed. Explicit scopes in one batch (Chronicle used directly) |
| Not | Two aggregates, each guarding only its own source. One model that folds both entities: it is refused |

## A member and a book

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Monads;

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
    Isbn Id,
    [Increment<BookAdded>, Decrement<BookBorrowed>] int Available);

public record MemberLoans(
    MemberId Id,
    [Increment<LoanTaken>, Decrement<LoanReturned>] int Active);

[Command]
public record BorrowBook(Isbn Isbn, MemberId Member) : ICanProvideEventSourceId
{
    public const int MaximumLoans = 3;

    public EventSourceId GetEventSourceId() => Isbn;

    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(
        [LookUpBy(nameof(Isbn))] BookStock? stock,
        [LookUpBy(nameof(Member))] MemberLoans? loans)
    {
        if (stock is not { Available: > 0 })
        {
            return ValidationResult.Error("No copies left.");
        }

        if (loans is { Active: >= MaximumLoans })
        {
            return ValidationResult.Error("The member has reached the loan limit.");
        }

        return new[]
        {
            new EventForEventSourceId(Isbn, new BookBorrowed(Member)),
            new EventForEventSourceId(Member, new LoanTaken(Isbn))
        };
    }
}
```

```text
guard isbn:   types=[BookAdded, BookBorrowed], after <tail before the BookStock read>
guard member: types=[LoanTaken, LoanReturned], after <tail before the MemberLoans read>
```

The rule needs both models and returns events for two entities, so it is in `Handle()`, which returns a `Result` that declines before anything is appended. Both guards are checked in the one atomic append. If either side changed, nothing is written and the message names the model that changed.

Things to get right:

- The command appends to two entities, so it returns `EventForEventSourceId` for each. It also implements `ICanProvideEventSourceId` to say which entity is its subject, because the analyzer does not see through a `Result` return type ([ARCCHR0002](../../code-analysis/ARCCHR0002.md)).
- Each entity gets an event of its own type. `BookBorrowed` goes to the book and `LoanTaken` goes to the member. Do not write the same event type to both: each model would fold the other entity's copy.
- The model of each entity must be keyed by that entity and folded from events appended to it. `MemberLoans` counts `LoanTaken` on the member's own stream.
- A model's `Id` has the same type as the command property `[LookUpBy]` names. `BookStock` has an `Isbn Id` and the command's `Isbn` property is an `Isbn`; [ARCCHR0031](../../code-analysis/ARCCHR0031.md) reports a mismatch.

### The trap: a model keyed by an event property

It is tempting to build `MemberLoans` from the book's events, keyed by the member named inside the event (`UsingKey(e => e.Member)`). Chronicle refuses that model, because a conflict on the member cannot be detected from the book's stream. The command fails with:

```text
BorrowBook reads 'loans' (MemberLoans) keyed by an event property, so read protection cannot guard it. If it is only context, mark it [Unprotected("why")]. Otherwise append a fact to the entity it is keyed by, or use a constraint.
```

The fix is the example above: append `LoanTaken` to the member.

## A course and a student

Course capacity and a per-student limit. This is the same pattern, with a command whose subject is the course.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Monads;

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
    CourseId Id,
    int Capacity,
    [Increment<SeatTaken>] int Taken);

public record StudentCourses(
    StudentId Id,
    [Increment<CourseJoined>] int Count);

[Command]
public record SubscribeToCourse(CourseId Course, StudentId Student) : ICanProvideEventSourceId
{
    public const int MaximumCourses = 3;

    public EventSourceId GetEventSourceId() => Course;

    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(
        [LookUpBy(nameof(Course))] CourseSeats? course,
        [LookUpBy(nameof(Student))] StudentCourses? student)
    {
        if (course is null)
        {
            return ValidationResult.Error("The course does not exist.");
        }

        if (course.Taken >= course.Capacity)
        {
            return ValidationResult.Error("The course is full.");
        }

        if (student is { Count: >= MaximumCourses })
        {
            return ValidationResult.Error("The student is already in three courses.");
        }

        return new[]
        {
            new EventForEventSourceId(Course, new SeatTaken(Student)),
            new EventForEventSourceId(Student, new CourseJoined(Course))
        };
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
