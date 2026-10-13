---
title: Validators and protected decisions
description: What a CommandValidator can and cannot do for a rule that depends on state, why it never guards the rule, and what a protected decision accepts.
---

Use this page when you are about to put a state check in a `CommandValidator<T>`, or when a protected command fails because of a validator. A validator checks the shape of the input. It is not where a stateful rule is made to hold.

## Where each kind of rule goes

| Rule | Put it in | Why |
| --- | --- | --- |
| The input is well formed (a name is not empty, a number is in range) | A validator without dependencies, data annotations or a `ConceptValidator<T>` | It needs no state, so there is nothing to race |
| A value is unique | A constraint with `.WithMessage(...)` | The kernel enforces it for every writer and Arc returns your message |
| The outcome depends on stored state | `Handle()` or `Provide()` of a `[ProtectedDecision]` command, returning `Result<TEvent, ValidationResult>` | The read is a decision read, so it is guarded |
| The user should see a hint before submitting | A query, or an advisory validator over a plain read | It is a hint; the rule is enforced elsewhere |

## What a validator can see

A validator can take any service, including the read model Arc resolves for the command's key, as a constructor parameter. That read model is a **plain read**: Arc loads it from Chronicle's read model store before the validator runs.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

[EventType]
public record CopyAdded();

[EventType]
public record BookBorrowed(Guid MemberId);

public record BookStock(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>] int Available);

[Command]
[Unprotected]
public record BorrowBookWithHint(EventSourceId BookId, Guid MemberId)
{
    public BookBorrowed Handle() => new(MemberId);
}

public class BorrowBookWithHintValidator : CommandValidator<BorrowBookWithHint>
{
    public BorrowBookWithHintValidator(BookStock? stock) =>
        RuleFor(command => command.BookId)
            .Must(_ => stock is { Available: > 0 })
            .WithMessage("No copies of this book are available.");
}
```

`[Unprotected]` on the command says that the plain read is deliberate; it silences [ARCCHR0011](../code-analysis/index.md#arcchr0011-unguarded-legacy-decision-reads). It does not make the read safe.

## A validator never guards the rule

The pipeline runs the filters, including validators, then `Provide()`, then `Handle()`, then commits. A plain read does nothing in that sequence to remember where the event log was when it was read. So with `BorrowBookWithHint`:

1. Two members submit at the same time. Both validators read `Available: 1`.
2. Both validators pass. Both `Handle()` methods return `BookBorrowed`.
3. Both events are appended. The default check only covers the window between the tail read at commit and the write.

The same applies to every plain read: a materialized projection (it can also lag), a passive projection or reducer (fresh at read time, but it captures nothing), and `IReadModels` called inside the validator. Reading the same model in `Handle()` instead does not change that: the validator and `Handle()` see the same scoped instance, and neither is guarded. The cure is not a different place for the plain read. It is a different kind of read.

### An advisory validator over a plain read

Keep a validator like `BorrowBookWithHintValidator` only when it is a hint and something else holds the rule: a constraint, an aggregate, a stream decision, an explicit scope, or a repair that you accept. Then the validator's job is a fast, friendly early answer, and a rare race ends in the guard rejecting the append.

If nothing else holds the rule, move it.

## Move the rule into a protected decision

Replace the validator's read with a `DecisionRead<T>` in `Handle()`, and return a `Result` that declines:

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
[ProtectedDecision]
public record BorrowBookDecision(EventSourceId BookId, Guid MemberId)
{
    public Result<BookBorrowed, ValidationResult> Handle(DecisionRead<BookStock> stock) =>
        stock.Instance is not { Available: > 0 }
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(MemberId);
}
```

The message reaches the client the same way a validator's message does. If two members race, the second receives a validation result with the reason `concurrencyViolation` and a message that the book "changed after it was read". A decision read works only if the model is admitted: a flat, event-source-keyed projection with an `Id` property, not a reducer. If it is not, use a stream decision, an aggregate or a constraint. The [decision matrix](./index.md#decision-matrix-by-rule-shape) lists the choices.

## What a protected command accepts

A `[ProtectedDecision]` command runs a discoverable validator (`CommandValidator<T>`, `ConceptValidator<T>` or any `IDiscoverableValidator<T>` in the command's graph) only if the validator has **exactly one public constructor, and that constructor has no parameters**. Any constructor dependency is refused: a read model, `DecisionRead<T>`, `IDecisionReads`, `ILogger`, `ICommandContext`. So is a validator with more than one public constructor, and a validator instance that the service provider supplies and Arc did not construct, or whose rules changed after Arc constructed it.

```csharp
using Cratis.Arc.Commands;
using FluentValidation;

public class BorrowBookDecisionValidator : CommandValidator<BorrowBookDecision>
{
    public BorrowBookDecisionValidator() =>
        RuleFor(command => command.MemberId)
            .NotEmpty()
            .WithMessage("A member is required.");
}
```

This validator is parameterless and runs. The one below is refused:

```csharp
using Cratis.Arc.Commands;
using FluentValidation;

public class BorrowBookDecisionStockValidator : CommandValidator<BorrowBookDecision>
{
    public BorrowBookDecisionStockValidator(BookStock stock) =>
        RuleFor(command => command.BookId)
            .Must(_ => stock.Available > 0)
            .WithMessage("No copies of this book are available.");
}
```

What happens when a validator is refused:

- The command fails with an error, which the client sees as an HTTP 500 response. It is not a validation failure.
- Neither `Provide()` nor `Handle()` runs, and the unit of work rolls back.
- The `X-Allowed-Severity` request header cannot allow it.
- The same refusal applies to the `{url}/validate` endpoint.
- There is no build-time diagnostic for the refusal. You find it when the command runs, so exercise every protected command in a scenario.

[ARCCHR0011](../code-analysis/index.md#arcchr0011-unguarded-legacy-decision-reads) reports Chronicle-backed constructor parameters of a `CommandValidator<T>` on an event-producing `[Command]` at Info severity, and its message points at `Provide()` and `Handle()`. It does not report other validator base types, and a quiet analyzer does not mean the validator is accepted. [ARC0006](../../code-analysis/ARC0006.md) warns on non-nullable parameters of `[ReadModel]` types and `IProjectionFor<T>` targets, validators included. It is about absence, not about guarding.

### What Arc certifies, and what it does not

Arc certifies two things about an accepted validator: its only public constructor is parameterless, and Arc ran it, so the instance still has the rules and rule components that the constructor gave it. A decorator or any code that adds, removes or replaces a rule on Arc's instance is refused.

It does not certify that the rules read nothing but their input. Keeping validators free of decision state is your responsibility, including:

- static members and service locators read by a rule;
- property injection, or code that resolves the validator and sets a field a rule reads;
- `ApplyCondition` on an existing rule, a changed cascade mode, or rules added to a nested child validator.

Custom `ICommandFilter` implementations are not checked at all in protected mode. Do not read decision state in one.

### The validate endpoint

`{url}/validate` runs the filters and nothing else: no `Provide()`, no `Handle()`, no unit of work. A successful validation does not promise that executing the command will commit. The next execution folds fresh state. Decision reads on the validate path are real folds that are not enrolled, because nothing commits.

## Testing

`CommandScenario<T>().UseDecisionReads()` applies the same validator rule: parameterless validators run and validators with constructor dependencies are refused. Seed real events, because a pinned read model cannot supply a guard. See [protected decision scenarios](../../testing/chronicle.md#protected-decision-scenarios).

## Where to go next

- [Read models in commands](../read-models/injecting-into-commands.md): nullability, `Provide()` and the full decision-read contract.
- [Pitfalls](./pitfalls.md): the other ways a rule fails without an error.
- [Command validation](../../commands/validation.md): Arc's validation model in general.
