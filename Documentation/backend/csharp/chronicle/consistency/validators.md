---
title: Validators and read protection
description: Put a state-dependent rule in a CommandValidator with the read model as a constructor parameter. Under On the read is guarded; under Off it is a hint.
---

Use this page when you write a rule such as "no copies left" or "the member has too many loans". Under read protection `On`, the place for that rule is a `CommandValidator<T>` that takes the read model as a constructor parameter. `Handle()` then says only what happens.

## Where each kind of rule goes

| Rule | Put it in | Why |
| --- | --- | --- |
| The input is well formed (a name is not empty, a number is in range) | A validator without state, data annotations or a `ConceptValidator<T>` | It needs no state, so there is nothing to race |
| A value is unique | A constraint with `.WithMessage(...)` | The kernel enforces it for every writer. Arc returns your message |
| The command may proceed only if stored state allows it | A `CommandValidator<T>` with the read model as a constructor parameter, under `On` | The parameter is a protected read, so the approval is guarded |
| The rule needs a value computed inside `Handle()` | `Handle()` returning `Result<TEvent, ValidationResult>` | The read parameter of `Handle()` is protected too |
| The user should see a hint before submitting | A query, or a validator over an `[Unprotected]` read | It is a hint. The rule is enforced elsewhere |

## A validator with a read model

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
public record CopyAdded();

[EventType]
public record BookBorrowed(MemberId Member);

public record BookStock(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>] int Available);

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
            .WithMessage("No copies of this book are available.");
}
```

Under `On` this is guarded:

1. Arc starts the command's unit of work and turns on read protection before the filters run.
2. Arc constructs the validator inside that unit. Its `BookStock` parameter is a protected read: Chronicle captures the position, folds the model, and enrols the guard before the validator sees the value.
3. The validator approves. `Handle()` returns the event. `Handle()` and the validator share the same instance and the same guard.
4. At commit, if a `CopyAdded` or `BookBorrowed` for that book arrived after the position captured in step 2, nothing is written. The caller gets a `400` with the reason `concurrencyViolation` and the detail `ReadChanged`.

A validator that rejects writes nothing, so a rejection needs no guard and is never retried by Arc.

The message reaches the client the same way as any validator message. The same rules apply to a nullable parameter: use `BookStock?` when absence is a business condition, and a non-nullable parameter when the command cannot run without the model. See [Nullable means you handle absence](../read-models/injecting-into-commands.md#nullable-means-you-handle-absence).

## A validator for one stream

A plain constructor parameter whose model declares `[EventStream<TSource>(stream)]` reads the command's own stream, including its resolved stream-id template. Under `On`, validator and `Handle()` share one enrolled fold. Under `Off` or `Measure`, the fold stays stream-scoped but is unguarded. A missing template part returns `400` naming the property before any read; a mismatched route is a configuration error in every mode. Do not use `[LookUpBy]` to redirect a per-stream parameter.

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | [Per-stream model and validator](./scenarios/per-stream.md#a-monthly-limit) |
| The entity's whole history, such as a balance | Whole-source model. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Rich behaviour across the whole entity | Aggregate, without reads of the same source |
| Another month or another source | `IDecisionReads` stream overload in `Provide()` |
| Uniqueness across sources | Constraint |

A validator runs before `Provide()`. Do not also acquire a later stream decision for the same source: its boundary may not cover the validator's earlier read. [ARCCHR0036](../code-analysis/ARCCHR0036.md) warns about that combination.

## Several keys in one validator

Put `[LookUpBy(nameof(Command.Property))]` on each constructor parameter. The name is resolved on the command, not on the validator. Each read gets its own guard, and `Handle()` can return one `EventForEventSourceId` for each entity without implementing `ICanProvideEventSourceId`. See [Two entities](./scenarios/two-entities.md) and [the transfer example](./reads.md#choose-the-key-with-lookupby).

## What a validator may and may not do under On

| In a validator | Under `On` |
| --- | --- |
| A read-model constructor parameter | Protected |
| `[Unprotected("why")]` on the parameter or the model | Advisory. It guards nothing |
| `IReadModels`, `IEventStore` or `IEventLog` used in a rule | Refused. Take the model as a parameter instead |
| `IMongoCollection<T>` of a Chronicle-backed model | Refused. Use `[Unique]` for uniqueness |
| A service such as a price list | Unknown. It is outside the guarantee |
| A validator registered by hand as scoped, singleton, a factory or an instance | Refused. Let convention discovery register it, which makes it transient |

The last row is the one that surprises people. Under `On`, Arc builds a fresh validator for every command so that its read-model parameters belong to that command's unit. A validator that the container supplies from another registration is refused with this error:

```text
Discoverable validator 'BorrowBookValidator' cannot run while read protection is On because Arc did not construct it for this invocation. Let convention discovery register it as transient; do not use a scoped, singleton, factory or instance registration.
```

Arc checks the registration the container actually uses, which is the last one. Registering a validator again by hand after `AddCratisArc()` replaces the convention registration, so that validator is refused under `On` unless the new registration is again a plain transient binding of the validator to itself.

Other constructor parameters, such as `ILogger<T>`, are resolved from the container.

## The validate endpoint

`{url}/validate` runs the filters and validators, and nothing else: no `Provide()`, no `Handle()`, no commit. Reads there are fresh, but nothing is guarded or written, because nothing commits. A successful validation does not promise that executing the command will succeed. The next execution reads again and guards again.

That makes a validator the right place for early feedback: the user sees "no copies left" as soon as the form is validated, and the same rule holds when the command executes.

## Under Off and Measure

Under `Off` the validator runs as it always did, and its read-model parameter is a plain read. It is a hint:

1. Two members submit at the same time. Both validators read `Available: 1`.
2. Both validators pass. Both `Handle()` methods return `BookBorrowed`.
3. Both events are appended.

A validator over a plain read is acceptable only when something else holds the rule: a constraint, an aggregate, a stream decision, an explicit scope, or a repair you accept. Mark the command's read `[Unprotected("why")]` to say so. [ARCCHR0011](../code-analysis/ARCCHR0011.md) reports plain reads in event-producing commands under `Off`.

`Measure` behaves like `Off` for the validator, and reports what `On` would do.

### Commands marked [ProtectedDecision]

A `[ProtectedDecision]` command that is not under `On` keeps the earlier rule: Arc runs a discoverable validator only if it has exactly one public constructor and that constructor has no parameters, and refuses any other, because a plain read there would not be guarded. Under `On`, a validator with read-model parameters is allowed, as described above. If you are moving such a command to `On`, move its state checks from `Handle()` to a validator, and drop `DecisionRead<T>` for a plain parameter.

### What Arc does not check

Arc certifies what it can see: the validator was constructed for this invocation. It does not certify that its rules read nothing but the parameters you declared. Keep these out of the validators of a protected command:

- static members and service locators read by a rule;
- property injection, or code that resolves the validator and sets a field a rule reads;
- a decorator that returns the inner instance after changing its state;
- `ApplyCondition` on an existing rule, a changed cascade mode, or rules added to a nested child validator.

Custom `ICommandFilter` implementations are not checked at all. Do not read decision state in one.

## Where to go next

- [Reads](./reads.md): what each read becomes under `On`.
- [Scenarios](./scenarios/index.md): the same pattern for each rule shape.
- [Pitfalls](./pitfalls.md): what fails silently.
- [Command validation](../../commands/validation.md): Arc's validation model in general.
- [Choosing how a rule holds in Chronicle](/chronicle/dynamic-consistency-boundary/choosing/): why a check before the append is not the guard.
