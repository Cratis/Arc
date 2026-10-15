---
title: Less idiomatic Arc
description: Shapes that work in Arc but are not guarded by themselves, such as an injected IEventLog, IReadModels in Handle, several event sources and the earlier DecisionRead form, and how to bring each one back to the idiomatic form.
---

Use this page when your command is not a plain `[Command]` that returns events. These shapes work, and Arc supports them. Under read protection `On`, some are refused. Each section says what the shape does and how to move it to the [idiomatic form](./index.md#fully-idiomatic-arc).

All examples on this page share one namespace.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

public record BookId(string Value) : EventSourceId(Value);

public record MemberId(string Value) : EventSourceId(Value);

[EventType]
public record CopyAdded();

[EventType]
public record BookBorrowed(MemberId Member);

[EventType]
public record MemberBorrowedBook(BookId Book);

[EventType]
public record ReadingImported(double Value);

public record BookStock(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>] int Available);
```

## An injected event log

Injecting `IEventLog` and calling `Append` writes at once. The append reads the tail when you call it, ignores the command's unit of work and any guard, and stays in the log if the command later fails.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public record SensorId(string Value) : EventSourceId(Value);

[Command]
public record ImportReading(SensorId Sensor, double Value)
{
    public async Task Handle(IEventLog eventLog) =>
        await eventLog.Append(Sensor, new ReadingImported(Value));
}
```

| Mode | What happens |
| --- | --- |
| `Off`, `Measure` | Written at once. A successful append cannot be undone. [ARCCHR0007](../../code-analysis/ARCCHR0007.md) reports the `IEventLog` parameter |
| `On` | Refused with `ImmediateAppendWhileReadsAreProtected`. [ARCCHR0012](../../code-analysis/ARCCHR0012.md) reports it as a warning |

The idiomatic form returns the event: `public ReadingImported Handle() => new(Value);`. If you must append inside `Handle()`, stage the event in the command's unit of work with `eventLog.Transactional.Append(...)`. It commits in one atomic append with the events you return and under the same guards:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using FluentValidation;

[Command]
public record BorrowBook(BookId Book, MemberId Member) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => Book;

    public async Task<BookBorrowed> Handle(IEventLog eventLog)
    {
        await eventLog.Transactional.Append(Member, new MemberBorrowedBook(Book));
        return new BookBorrowed(Member);
    }
}

public class BorrowBookValidator : CommandValidator<BorrowBook>
{
    public BorrowBookValidator(BookStock? stock) =>
        RuleFor(command => command.Book)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies of this book are available.");
}
```

[ARCCHR0007](../../code-analysis/ARCCHR0007.md) still reports the `IEventLog` parameter, because the rule matches the parameter type and not the call. A narrow suppression is acceptable for a deliberate transactional append. Outside a command or another established unit of work, `Transactional.Append` throws.

## Plain read models through IReadModels

`IReadModels.GetInstanceById<T>(...)` returns state and captures no boundary. Under `Off` it works and guards nothing:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
public record LendBook(BookId Book, MemberId Member) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => Book;

    public async Task<Result<BookBorrowed, ValidationResult>> Handle(IReadModels readModels)
    {
        var stock = await readModels.GetInstanceById<BookStock>(Book);
        return stock is not { Available: > 0 }
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(Member);
    }
}
```

| Mode | What happens |
| --- | --- |
| `Off` | Two commands can both read `Available: 1` and both succeed. The projection may also lag the log. [ARCCHR0011](../../code-analysis/ARCCHR0011.md) reports it at Info severity |
| `Measure` | Reported as `refused_read` |
| `On` | Refused with `ReadRefusedWhileReadsAreProtected`. [ARCCHR0028](../../code-analysis/ARCCHR0028.md) reports it |

Take the model as a parameter instead: a validator with `BookStock?`, as in [one entity](./single-entity.md). If the read is deliberately a hint, mark the method `[Unprotected("why")]` and enforce the rule elsewhere.

## The earlier form: [ProtectedDecision] and DecisionRead

Before read protection had modes, a command opted in one at a time with `[ProtectedDecision]` and took `DecisionRead<T>`. That form still works, and under `On` it is redundant.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
[ProtectedDecision]
public record BorrowBookDecision(BookId Book, MemberId Member) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => Book;

    public Result<BookBorrowed, ValidationResult> Handle(DecisionRead<BookStock> stock) =>
        stock.Instance is not { Available: > 0 }
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(Member);
}
```

Use it when a project stays `Off` and one command needs a guard now. Move to a plain parameter and a validator when you turn the project `On`: a `[ProtectedDecision]` command outside `On` refuses validators with constructor dependencies, and a validator is where the rule belongs. See [Validators](../validators.md#commands-marked-protecteddecision).

## Several event sources

Returning `EventForEventSourceId` values writes to several event sources in one atomic append. Without reads or explicit scopes, each source gets the default check taken at commit.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

public record CourseId(string Value) : EventSourceId(Value);

[EventType]
public record StudentEnrolled(CourseId Course, MemberId Student);

[Command]
public record EnrollStudent(CourseId Course, MemberId Student) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => Course;

    public IEnumerable<EventForEventSourceId> Handle()
    {
        var enrolled = new StudentEnrolled(Course, Student);
        return [new(Course, enrolled), new(Student, enrolled)];
    }
}
```

The batch is atomic: either both events are written or neither. Nothing here checks a capacity. To add the rule, read both keys as in [two entities](./two-entities.md). Aggregates for each key do not give you that: each aggregate guards only its own source.

## Controllers

An MVC controller action is not a model-bound `[Command]`. It does not get Arc's command unit of work, so read protection does not apply. Write it as in [Chronicle used directly](./chronicle-direct.md#a-controller-action).
