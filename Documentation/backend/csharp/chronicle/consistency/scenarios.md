---
title: Scenarios for rules that hold
description: The same rule shapes written three ways - fully idiomatic Arc, less idiomatic Arc, and Chronicle used directly inside an Arc app - with what each one guards.
---

Use this page when you know the shape of your rule (see the [decision matrix](./index.md#decision-matrix-by-rule-shape)) and want the code. Every scenario states the rule, shows a complete example, and says what holds and what does not.

| Level | You write | Rules that hold |
| --- | --- | --- |
| [1. Fully idiomatic Arc](#level-1-fully-idiomatic-arc) | `[Command]` records that return events, constraints, `[ProtectedDecision]`, stream decisions, aggregates | All of them |
| [2. Less idiomatic Arc](#level-2-less-idiomatic-arc) | An injected `IEventLog`, `IReadModels` in `Handle()`, returned events for several event sources | Only what you add: these shapes are not guarded by themselves |
| [3. Chronicle used directly](#level-3-chronicle-used-directly-in-an-arc-app) | `IEventStore`, `IEventLog`, `IDecisionReads` and explicit scopes in endpoints, controllers and services | As strong as the code you write: nothing is enrolled for you |

Each example is a complete file. All examples on this page share one namespace, so the types they declare are visible to later examples.

## Level 1: fully idiomatic Arc

### A unique value

**Rule:** no two members register with the same email address.

Declare a constraint. The kernel enforces it for every writer, and Arc turns a violation into a validation error that carries your message.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
public record MemberRegistered(string Email);

public class UniqueMemberEmail : IConstraint
{
    public void Define(IConstraintBuilder builder) => builder
        .Unique(_ => _
            .On<MemberRegistered>(e => e.Email)
            .WithMessage("A member with this email address is already registered."));
}

[Command]
public record RegisterMember(string Email)
{
    public MemberRegistered Handle() => new(Email);
}
```

`RegisterMember` has no event source id property, so each call creates a new member stream. That is right for a creation command: the constraint, not the stream, makes the email unique. A validator or a read model that asks "does this email exist" first has a gap between the check and the append; the constraint does not. See [pitfalls](./pitfalls.md#a-uniqueness-check-instead-of-a-constraint).

A constraint is a fixed rule. If a member can change email, add the changed event with `.On<MemberEmailChanged>(...)` and release the old value with `.RemovedWith<MemberRemoved>()`.

### Created once

**Rule:** a member has at most one library card.

Put `[Unique]` on the event class: one event of that type per event source.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;

[EventType]
[Unique(message: "This member already has a library card.")]
public record LibraryCardIssued();

[Command]
public record IssueLibraryCard(EventSourceId Member)
{
    public LibraryCardIssued Handle() => new();
}
```

`Member` is the command's event source id, so the card is recorded on the member's own stream and a second card for the same member is rejected by the kernel.

### A count on one key

**Rule:** a member can have at most three loans at once.

The facts live on the member's stream, the model counts them, and the command reads the model as a protected decision.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventType]
public record LoanTaken(string Isbn);

[EventType]
public record LoanReturned(string Isbn);

public record MemberLoans(
    EventSourceId Id,
    [Increment<LoanTaken>, Decrement<LoanReturned>] int Active);

[Command]
[ProtectedDecision]
public record TakeLoan(EventSourceId Member, string Isbn)
{
    public const int MaximumLoans = 3;

    public Result<LoanTaken, ValidationResult> Handle(DecisionRead<MemberLoans> loans) =>
        loans.Instance is { Active: >= MaximumLoans }
            ? ValidationResult.Error("The member has reached the loan limit.")
            : new LoanTaken(Isbn);
}
```

Three things make this hold:

- `Member` is the only event source id on the command, so it is the key of the decision read. The model is read from the member's stream, and `LoanTaken` is appended to that same stream.
- `MemberLoans` has an `Id` property. Decision reads match the key to it.
- A missing instance means no loans yet, so the handler treats `null` as zero.

The test seeds events and queues a competing loan. `AppendConcurrently` appends it after the command read its state and before the command commits:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Xunit;

public class when_taking_a_loan_while_another_loan_is_taken
{
    [Fact]
    public async Task should_reject_the_command_with_a_concurrency_violation()
    {
        await using var scenario = new CommandScenario<TakeLoan>().UseDecisionReads();
        var member = EventSourceId.New();
        scenario.Given.ForEventSource(member).Events(new LoanTaken("978-0-00-000001-0"));
        scenario.AppendConcurrently(member, new LoanTaken("978-0-00-000002-0"));

        var result = await scenario.Execute(new TakeLoan(member, "978-0-00-000003-0"));

        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
        Assert.Empty(scenario.AppendedEvents);
    }
}
```

A stock level on one book works the same way, with `Increment` and `Decrement` on the book's own events; the [idiomatic path](./index.md#the-idiomatic-path) shows it.

### A rule spanning two keys

**Rule:** a course has a capacity, and a student can join at most three courses.

The two limits live on two event sources, so read both and write one event to each in one batch. Chronicle appends the batch atomically, and each read guards its own key.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventType]
public record CourseDefined(int Capacity);

[EventType]
public record StudentSubscribed(EventSourceId CourseId, EventSourceId StudentId);

[EventType]
public record StudentUnsubscribed(EventSourceId CourseId, EventSourceId StudentId);

[FromEvent<CourseDefined>]
public record CourseSeats(
    EventSourceId Id,
    int Capacity,
    [Increment<StudentSubscribed>, Decrement<StudentUnsubscribed>] int Taken);

public record StudentLoad(
    EventSourceId Id,
    [Increment<StudentSubscribed>, Decrement<StudentUnsubscribed>] int Courses);

[Command]
[ProtectedDecision]
public record SubscribeToCourse(EventSourceId CourseId, EventSourceId StudentId) : ICanProvideEventSourceId
{
    public const int MaximumCourses = 3;

    public EventSourceId GetEventSourceId() => CourseId;

    public async Task<Result<IEnumerable<EventForEventSourceId>, ValidationResult>> Handle(
        DecisionRead<CourseSeats> course,
        IDecisionReads reads)
    {
        var student = await reads.Get<StudentLoad>((ReadModelKey)StudentId.Value);

        if (course.Instance is not { } seats)
        {
            return ValidationResult.Error("The course does not exist.");
        }

        if (seats.Taken >= seats.Capacity)
        {
            return ValidationResult.Error("The course is full.");
        }

        if (student.Instance is { Courses: >= MaximumCourses })
        {
            return ValidationResult.Error("The student is already in three courses.");
        }

        var subscribed = new StudentSubscribed(CourseId, StudentId);
        return new[]
        {
            new EventForEventSourceId(CourseId, subscribed),
            new EventForEventSourceId(StudentId, subscribed)
        };
    }
}
```

- `ICanProvideEventSourceId` selects the course as the command's event source id, so the injected `DecisionRead<CourseSeats>` reads the course. Two `EventSourceId` properties without it raise [ARCCHR0002](../code-analysis/index.md#arcchr0002-ambiguous-command-identity).
- `IDecisionReads.Get<T>` reads the student by its own key. Both reads are enrolled in the command's unit of work.
- `StudentSubscribed` is written to both streams, because an event has exactly one event source id. Each model folds the copy on its own stream.
- A decision read on a key guards that key even when the command writes only to another one: a competing subscription for the student conflicts with this command.

The test queues a competing subscription on the student's stream:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Xunit;

public class when_subscribing_while_the_student_joins_another_course
{
    [Fact]
    public async Task should_reject_the_command_with_a_concurrency_violation()
    {
        await using var scenario = new CommandScenario<SubscribeToCourse>().UseDecisionReads();
        var course = EventSourceId.New();
        var student = EventSourceId.New();
        scenario.Given.ForEventSource(course).Events(new CourseDefined(30));
        scenario.AppendConcurrently(student, new StudentSubscribed(EventSourceId.New(), student));

        var result = await scenario.Execute(new SubscribeToCourse(course, student));

        result.ShouldHaveValidationErrorBecauseOf(ValidationResultReason.ConcurrencyViolation);
    }
}
```

This guards exactly the keys you read. If a rule's set of keys can grow, such as a limit across loans that each live on their own book stream, a new key enters nobody's guard. Model such a limit on one key that carries all the facts, as `MemberLoans` does.

### An editing conflict

**Rule:** a rename is rejected if the document changed after the editor loaded it.

The client sends the revision it saw. Return an explicit scope with that revision, and nothing the command reads can replace it.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

[EventType]
public record DocumentTitleChanged(string Title);

[Command]
public record RenameDocument(EventSourceId DocumentId, string Title, ulong ExpectedRevision)
{
    public EventsWithConcurrencyScopes Handle() => new(
        [new(DocumentId, new DocumentTitleChanged(Title))],
        [new(DocumentId, new ConcurrencyScope(
            new EventSequenceNumber(ExpectedRevision),
            EventSourceId: DocumentId,
            EventTypes: [typeof(DocumentTitleChanged).GetEventType()]))]);
}
```

The scope fails the append if any `DocumentTitleChanged` for this document has a sequence number above `ExpectedRevision`. How the client learns the revision is part of your read model and query. If `Handle()` also read plain read models, mark the command `[Unprotected]` to say so; the explicit scope is the guard, and Chronicle refuses to combine it with a decision read on the same event source.

`concurrency: true` on `[EventStreamId]` and its siblings is not a substitute. It reads the tail *after* `Handle()`, so it can never carry a revision the user saw. See [Concurrency](../commands/concurrency.md).

### One stream of raw events

**Rule:** a report period is approved once, and a repeat of the same approval is not an error.

Read the routed stream with `IStreamReads` and return `decision.Append(...)`. The tail is captured before the events are read, and the returned scope covers the whole stream.

```csharp
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record PeriodApproved();

[Command]
[EventSourceType("reports")]
[EventStreamType("approval")]
[EventStreamId("{Period}")]
public record ApprovePeriod(EventSourceId ReportId, string Period)
{
    public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand();

    public EventsWithConcurrencyScopes Handle(StreamDecision decision) =>
        decision.Has<PeriodApproved>()
            ? decision.Append([])
            : decision.Append(new PeriodApproved());
}
```

A competing event in that stream after the read rejects the command, including when both commands try the first append. The whole stream crosses the wire on every command, so use this for short streams. See [Stream decisions](../commands/stream-decisions.md).

This example routes with string attributes. To route the same command through an event source definition instead, declare `[EventSource<TSource>(stream)]`; [the scenarios for each stream shape](../commands/event-source-definition-scenarios.md#decide-against-the-stream) show this decision that way, and [Chronicle's Dynamic Consistency Boundary scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) show the Chronicle side.

### Rich behaviour on one entity

**Rule:** a copy of a book is lent to one member at a time, and further rules about the copy will follow.

Use an aggregate root. It rebuilds its state from the events it handles, and Arc commits its events with the tail captured when it was resolved.

```csharp
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

public record MemberId(Guid Value) : ConceptAs<Guid>(Value);

[EventType]
public record CopyLent(MemberId MemberId);

[EventType]
public record CopyReturned();

public class Copy : AggregateRoot
{
    bool _lent;

    public async Task Lend(MemberId memberId)
    {
        if (_lent)
        {
            Failed("The copy is already lent.");
            return;
        }

        await Apply(new CopyLent(memberId));
    }

    public void OnCopyLent(CopyLent @event) => _lent = true;

    public void OnCopyReturned(CopyReturned @event) => _lent = false;
}

[Command]
public record LendCopy(EventSourceId CopyId, MemberId MemberId)
{
    public async Task<AggregateRootCommitResult> Handle(Copy copy)
    {
        await copy.Lend(MemberId);
        return await copy.Commit();
    }
}
```

Return the result of `Commit()`. A handler that awaits `Commit()` and ignores its result lets a rejected command report success. Do not also return the events the aggregate applied.

An aggregate guards its own event source and the event types it handles. It does not guard a rule across aggregates, and it cannot be combined with `[ProtectedDecision]` reads: commit of an aggregate throws once the command has enrolled a decision read. It also has no snapshots, so every command reads the handled events of the key again. See [Aggregate roots in commands](../aggregates/injecting-into-commands.md).

### A rare violation is acceptable

**Rule:** a flight may be overbooked briefly. A reactor cancels the reservations that exceed the capacity.

Do not guard the append. Observe the facts and append a correcting event.

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors;

[EventType]
public record SeatReserved(Guid ReservationId);

[EventType]
public record ReservationCancelled(Guid ReservationId, string Reason);

public class OverbookingRepair(IEventStore eventStore) : IReactor
{
    const int Capacity = 100;

    [OnceOnly]
    public async Task<ReservationCancelled?> Reserved(SeatReserved @event, EventContext context)
    {
        var events = await eventStore.EventLog.GetFromSequenceNumber(
            EventSequenceNumber.First,
            context.EventSourceId);
        var reservedUpToThis = events.Count(_ =>
            _.Context.SequenceNumber <= context.SequenceNumber && _.Content is SeatReserved);

        return reservedUpToThis > Capacity
            ? new ReservationCancelled(@event.ReservationId, "The flight was overbooked.")
            : null;
    }
}
```

Returning `null` records nothing. `[OnceOnly]` keeps the repair out of replays; it does not make delivery exactly-once, so a redelivered event can run the handler again. Make the correction safe to repeat, and use a repair only when the correction itself is acceptable to the people affected: a reactor cannot undo an irreversible side effect, and the repair appends with the default scope.

### An advisory hint

**Rule:** show "no copies left" before the user submits.

A hint is a query or a validator over a plain read. It is fine as a hint, and it guards nothing. Keep the rule itself in a protected decision or a constraint. See [validators](./validators.md#an-advisory-validator-over-a-plain-read).

### External data

**Rule:** a purchase uses the current price, and a book can be bought only while a copy is available.

`Provide()` fetches what Chronicle does not hold. The decision read guards your own state. The external value is not guarded, so put the price into the event: it then records what the decision used.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

public interface IPriceList
{
    Task<decimal> PriceOf(EventSourceId bookId);
}

public record QuotedPrice(decimal Amount);

[EventType]
public record CopyAdded();

[EventType]
public record CopySold(decimal Price);

public record CopiesInStock(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<CopySold>] int Available);

[Command]
[ProtectedDecision]
public record SellCopy(EventSourceId BookId)
{
    public async Task<QuotedPrice> Provide(IPriceList prices) => new(await prices.PriceOf(BookId));

    public Result<CopySold, ValidationResult> Handle(QuotedPrice price, DecisionRead<CopiesInStock> stock) =>
        stock.Instance is not { Available: > 0 }
            ? ValidationResult.Error("No copies are left.")
            : new CopySold(price.Amount);
}
```

### A hot key with a long history

**Rule:** a room accepts at most 20 bookings a month.

A decision read folds the key's history every time. Do not key the model on something that never stops growing: key it on the period that bounds the rule.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventType]
public record RoomBooked(Guid RoomId, DateOnly Day);

public record RoomMonth(EventSourceId Id, [Increment<RoomBooked>] int Bookings);

[Command]
[ProtectedDecision]
public record BookRoom(Guid RoomId, DateOnly Day) : ICanProvideEventSourceId
{
    public const int MaximumBookingsPerMonth = 20;

    public EventSourceId GetEventSourceId() => $"{RoomId:D}:{Day:yyyy-MM}";

    public Result<RoomBooked, ValidationResult> Handle(DecisionRead<RoomMonth> month) =>
        month.Instance is { Bookings: >= MaximumBookingsPerMonth }
            ? ValidationResult.Error("The room is fully booked this month.")
            : new RoomBooked(RoomId, Day);
}
```

Each month is a new event source, so each decision folds at most the bookings of one month. The key must not be blank or `*`, must be trimmed, and must not contain `#`.

## Level 2: less idiomatic Arc

These shapes work, and Arc supports them. None of them is guarded by itself. Each section says what the shape does and how to bring it back to Level 1.

### An injected event log

Injecting `IEventLog` and calling `Append` writes immediately. The append reads the tail at the moment you call it, ignores the command's unit of work and any decision read, and stays in the log if the command later fails. Arc watches the appends: a failed immediate append fails the command, but a successful one cannot be undone.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record ReadingImported(double Value);

[Command]
public record ImportReading(EventSourceId SensorId, double Value)
{
    public async Task Handle(IEventLog eventLog) =>
        await eventLog.Append(SensorId, new ReadingImported(Value));
}
```

This reports [ARCCHR0007](../code-analysis/index.md#arcchr0007-command-handler-injects-ieventlog) as a warning on `IEventLog` parameters. To get the Level 1 behaviour, return the event: `public ReadingImported Handle() => new(Value);`. If you must append inside `Handle()`, use `eventLog.Transactional.Append(...)`, which stages the event in the command's unit of work, next to the events you return:

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventType]
public record BookBorrowed(Guid MemberId);

[EventType]
public record MemberBorrowedBook(EventSourceId BookId);

[EventType]
public record BookReturned();

public record BookStock(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>, Increment<BookReturned>] int Available);

[Command]
[ProtectedDecision]
public record BorrowBook(EventSourceId BookId, Guid MemberId)
{
    public async Task<Result<BookBorrowed, ValidationResult>> Handle(
        DecisionRead<BookStock> stock,
        IEventLog eventLog)
    {
        if (stock.Instance is not { Available: > 0 })
        {
            return ValidationResult.Error("No copies of this book are available.");
        }

        await eventLog.Transactional.Append(MemberId.ToString("D"), new MemberBorrowedBook(BookId));
        return new BookBorrowed(MemberId);
    }
}
```

Both events commit in one atomic append, guarded by the decision read. [ARCCHR0007](../code-analysis/index.md#arcchr0007-command-handler-injects-ieventlog) still reports the `IEventLog` parameter in this shape, because the rule matches the parameter type rather than the call. A narrow suppression is acceptable for a deliberate transactional append; under warnings-as-errors suppress it on that command. A plain `eventLog.Append(...)` in the same method would be written before the commit, and survives a conflict that rejects the returned event: [ARCCHR0012](../code-analysis/index.md#arcchr0012-immediate-append-with-a-decision-read) reports it at Info severity. Outside a command or another established unit of work, `Transactional.Append` throws.

### Plain read models in `Handle()`

An injected read model or `IReadModels.GetInstanceById<T>(...)` returns state and captures no boundary. This works and guards nothing:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
public record LendBook(EventSourceId BookId, Guid MemberId)
{
    public async Task<Result<BookBorrowed, ValidationResult>> Handle(IReadModels readModels)
    {
        var stock = await readModels.GetInstanceById<BookStock>(BookId);
        return stock is not { Available: > 0 }
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(MemberId);
    }
}
```

[ARCCHR0011](../code-analysis/index.md#arcchr0011-unguarded-legacy-decision-reads) reports this at Info severity. Two commands can both read `Available: 1` and both succeed. The projection may also lag the event log. To fix it, mark the command `[ProtectedDecision]` and take `DecisionRead<BookStock>` as in the [idiomatic path](./index.md#the-idiomatic-path). The guard only sees the event types the model folds: `BookStock` decrements on `BookBorrowed`, which is why `LendBook` returns `BookBorrowed`. An event the model does not fold, such as a differently named lending event, is neither counted in `Available` nor detected as a competing append. If the read is deliberately only a hint, mark the command `[Unprotected]` and enforce the rule elsewhere.

### Several event sources

Returning `EventForEventSourceId` values writes to several event sources in one atomic append. Without decision reads or explicit scopes, each source gets the default check taken at commit.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

[EventType]
public record StudentEnrolled(EventSourceId CourseId, EventSourceId StudentId);

[Command]
public record EnrollStudent(EventSourceId CourseId, EventSourceId StudentId) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => CourseId;

    public IEnumerable<EventForEventSourceId> Handle()
    {
        var enrolled = new StudentEnrolled(CourseId, StudentId);
        return
        [
            new(CourseId, enrolled),
            new(StudentId, enrolled)
        ];
    }
}
```

The batch is atomic: either both events are written or neither. Nothing here checks a capacity. To add the rule, use the [two-key example](#a-rule-spanning-two-keys), which reads both keys and returns the same batch. Aggregates for each key do not give you that: each aggregate guards only its own source.

### Controllers

An MVC controller action is not a model-bound `[Command]`. It does not get Arc's command unit of work, so `[ProtectedDecision]` does not apply. Write it as in [Level 3](#a-controller-action).

## Level 3: Chronicle used directly in an Arc app

Here you use `IEventStore`, `IEventLog` and `IDecisionReads` yourself, in endpoints, controllers and services. Chronicle gives you the same guards; you enroll the reads, check the results and commit before you respond. Arc's command pipeline does none of that for you outside a command.

### A decision read on a detached append

Read a `GetDetached` decision read from the event store and pass it to `AppendMany` in `guardedBy`.

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Http;

[EventType]
public record BookIssued(Guid MemberId);

public record BookCopies(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookIssued>] int Available);

public static class IssueBook
{
    public static async Task<IResult> Handle(IEventStore eventStore, string bookId, Guid memberId)
    {
        var stock = await eventStore.GetDecisionReads().GetDetached<BookCopies>(bookId);
        if (stock.Instance is not { Available: > 0 })
        {
            return Results.UnprocessableEntity("No copies are available.");
        }

        var result = await eventStore.EventLog.AppendMany(
            [new EventForEventSourceId(bookId, new BookIssued(memberId))],
            guardedBy: [stock]);

        return result.IsSuccess
            ? Results.Ok()
            : Results.Conflict(result.GetDecisionConflicts([stock]));
    }
}
```

The boundary is captured before the fold, and the append is rejected if a `CopyAdded` or `BookIssued` arrived for the book since. `AppendMany` returns a result: a violation is a value, not an exception, so always check it. The batch must go to the event log of the same store and namespace, and needs at least one read.

### A unit of work with decision reads

Use `IDecisionReads.Get<T>` when the read and the append are in different places. The request's unit of work enrolls the read, you stage the event with `Transactional.Append`, and you commit before you respond.

```csharp
using Cratis.Chronicle.AspNetCore.Transactions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public record TakeLoanRequest(string Isbn);

public static class LoanEndpoints
{
    public static void Map(WebApplication app) =>
        app.MapPost("/members/{memberId}/loans", async (
            string memberId,
            TakeLoanRequest body,
            HttpContext http,
            IDecisionReads reads,
            IEventLog eventLog) =>
        {
            var loans = await reads.Get<MemberLoans>(memberId);
            if (loans.Instance is { Active: >= 3 })
            {
                return Results.UnprocessableEntity("The member has reached the loan limit.");
            }

            await eventLog.Transactional.Append(memberId, new LoanTaken(body.Isbn));

            var unit = await http.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync();
            return unit.IsSuccess ? Results.Ok() : Results.Conflict(unit.GetDecisionConflicts());
        });
}
```

Two parts matter:

- `Get<T>` needs an ambient unit of work. In an Arc app the Chronicle request middleware begins one per request.
- The middleware commits *after* the endpoint returns, and reports no concurrency violation. Commit with `IUnitOfWorkCompletionFeature.CommitAsync()` before responding and use the result, or the caller sees success for a write that was rejected.

Do not mix the two styles: a plain `eventLog.Append(...)` in the same request is written immediately and bypasses the guard.

### A controller action

A controller uses the same calls. It is not an Arc command, so the request unit of work and your code are all there is.

```csharp
using Cratis.Chronicle.AspNetCore.Transactions;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/members/{memberId}/loans")]
public class MemberLoansController(IDecisionReads reads, IEventLog eventLog) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Take(string memberId, TakeLoanRequest body)
    {
        var loans = await reads.Get<MemberLoans>(memberId);
        if (loans.Instance is { Active: >= 3 })
        {
            return UnprocessableEntity("The member has reached the loan limit.");
        }

        await eventLog.Transactional.Append(memberId, new LoanTaken(body.Isbn));

        var unit = await HttpContext.Features.Get<IUnitOfWorkCompletionFeature>()!.CommitAsync();
        return unit.IsSuccess ? Ok() : Conflict(unit.GetDecisionConflicts());
    }
}
```

### An explicit scope captured before the facts

When no decision-read model fits, capture the tail yourself, before you read the facts, and append with a scope built from it.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Microsoft.AspNetCore.Http;
using ScopeBuilder = Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScopeBuilder;

[EventType]
public record Deposited(decimal Amount);

[EventType]
public record Withdrawn(decimal Amount);

public static class Withdraw
{
    public static async Task<IResult> Handle(IEventLog eventLog, EventSourceId accountId, decimal amount)
    {
        var tail = await eventLog.GetTailSequenceNumber(accountId);
        var events = await eventLog.GetFromSequenceNumber(EventSequenceNumber.First, accountId);
        var balance = events.Select(_ => _.Content).Sum(content => content switch
        {
            Deposited deposited => deposited.Amount,
            Withdrawn withdrawn => -withdrawn.Amount,
            _ => 0m
        });

        if (balance < amount)
        {
            return Results.UnprocessableEntity("Insufficient funds.");
        }

        var scope = tail.IsUnavailable
            ? new ScopeBuilder().WithEventSourceId(accountId).ExpectingNoMatchingEvent().Build()
            : new ConcurrencyScope(tail, EventSourceId: accountId);

        var result = await eventLog.Append(accountId, new Withdrawn(amount), concurrencyScope: scope);
        return result.IsSuccess ? Results.Ok() : Results.Conflict();
    }
}
```

The order is the guard: the tail first, then the facts. Capture it after the facts and an event that arrived in between is invisible to the check. A `ConcurrencyScope` built without a sequence number, or with the builder but without `WithSequenceNumber` or `ExpectingNoMatchingEvent()`, is skipped by the kernel. In an Arc project, qualify Chronicle's `ConcurrencyScopeBuilder` (here with an alias): `Cratis.Arc.Chronicle.Commands` has a static class of the same name.

### A value claimed through the event source id

When a constraint does not fit, use the value as the event source id and append with an expected-empty scope for the claiming event.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Http;
using ScopeBuilder = Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScopeBuilder;

[EventType]
public record UserNameClaimed(Guid UserId);

public static class ClaimUserName
{
    public static async Task<IResult> Handle(IEventLog eventLog, string userName, Guid userId)
    {
        var scope = new ScopeBuilder()
            .WithEventSourceId(userName)
            .WithEventType<UserNameClaimed>()
            .ExpectingNoMatchingEvent()
            .Build();

        var result = await eventLog.Append(userName, new UserNameClaimed(userId), concurrencyScope: scope);
        return result.IsSuccess ? Results.Ok() : Results.Conflict("The user name is taken.");
    }
}
```

The scope detects any matching event appended after its boundary, whoever wrote it, but it protects only decisions that carry it: a writer that decides without a scope can still break the rule. A constraint is checked for every append, so prefer it where it fits.

## Where to go next

- [Validators](./validators.md): what a `CommandValidator<T>` can do next to these shapes.
- [Pitfalls](./pitfalls.md): what goes wrong silently, and the analyzer that reports it.
- [Route a command through an event source definition](../commands/event-source-definitions.md): where definitions and stream decisions fit.
- [Choosing how a rule holds in Chronicle](/chronicle/dynamic-consistency-boundary/choosing/): the Chronicle-only view of the same decisions, and its [scenarios](/chronicle/dynamic-consistency-boundary/scenarios/).
