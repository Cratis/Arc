---
title: Arc consistency with injected Chronicle services
description: "Injected Chronicle services do not by themselves guard an Arc command's decision. Stage appends and use read-model parameters to protect the batch."
---

Cratis Arc can use injected Cratis Chronicle services, but injecting a service does not by itself protect a command's decision. If your handler reads through `IReadModels` or appends through `IEventLog`, check when the operation runs and which guard covers it. The examples below explain the refusals under read protection `On` and the move to the [idiomatic form](./index.md#fully-idiomatic-arc).

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

The `IEventLog.Append` call below writes at once. Its concurrency check starts at append time, not at the earlier decision read. It does not join the command's unit of work or its guards, so a successful append stays in the log even if the command later fails.

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

Return the event instead: `public ReadingImported Handle() => new(Value);`. If you must append inside `Handle()`, stage the event in the command's unit of work with `eventLog.Transactional.Append(...)`. It commits atomically with the returned events. Under `On`, the validator's `BookStock` read guards that batch:

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

## Reading decision state in Handle()

`Handle(IDecisionReads reads)` and `Handle(DecisionRead<T> read)` expose acquisition and guard tokens to the decision. They remain supported for explicit advanced needs, but are not recommended for application code. Take the state as a plain parameter instead:

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Monads;

[Command, ReadProtection(ReadProtectionMode.On)]
public record BorrowAvailableBook(BookId Book, MemberId Member)
{
    public Result<BookBorrowed, ValidationResult> Handle(BookStock? stock) =>
        stock is not { Available: > 0 }
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(Member);
}
```

Use `[LookUpBy]` for a command property key, or a per-stream model for the command route. Only when neither can express the key should `Handle()` take `IDecisionReads` (or `DecisionRead<T>`) directly, with ARCCHR0040 suppressed with a reason and a link to the tracking feature. Application specs call `Handle(state)` directly or give that state to `CommandScenario`; they never configure decision reads or simulate concurrent appends. Arc does not add application test support for non-recommended handler patterns.

## The earlier form: [ProtectedDecision] and DecisionRead

Existing commands can opt in individually with `[ProtectedDecision]` and take `DecisionRead<T>`. That form still works. Under `On`, plain read-model parameters give the handler state without exposing the guard token, and validator constructor reads are protected too.

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

For new commands, put the state rule in a validator and take a plain read-model parameter under `On`. When migrating the example above, enable `On` before adding validator constructor dependencies: a `[ProtectedDecision]` command outside `On` refuses them. See [Validators](../validators.md#commands-marked-protecteddecision).

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
public record EnrollStudent(CourseId Course, MemberId Student)
{
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
