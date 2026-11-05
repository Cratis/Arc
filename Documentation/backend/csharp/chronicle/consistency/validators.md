---
title: Cratis Arc validators with protected reads
description: "Use a Cratis Arc CommandValidator with read-model parameters for state-dependent rules. On guards the reads; Off and Measure leave plain reads unguarded."
---

A `CommandValidator<T>` with a read-model constructor parameter lets Cratis Arc guard a state-dependent rule under read protection `On`. Use it for checks such as "a copy must be available" or "the member must be below the loan limit". Cratis Chronicle guards the state the validator approved, while `Handle()` returns the event that records the change.

The mode matters: `Off` leaves a plain read-model parameter unguarded, and `Measure` observes what `On` would do without adding that guard. A passing validator alone does not prevent another command from changing the state before yours commits.

## Where each kind of rule goes

| Rule | Put it in | Why |
| --- | --- | --- |
| The input is well formed (a name is not empty, a number is in range) | A validator without state, data annotations or a `ConceptValidator<T>` | It needs no state, so there is nothing to race |
| A value is unique | A constraint with `.WithMessage(...)` | The kernel enforces it for every writer. Arc returns your message |
| The command may proceed only if stored state allows it | A `CommandValidator<T>` with the read model as a constructor parameter, under `On` | The parameter is a protected read, so the approval is guarded |
| The rule needs a value computed inside `Handle()` | `Handle()` returning `Result<TEvent, ValidationResult>` | The read parameter of `Handle()` is protected too |
| The user should see a hint before submitting | A query, or a validator over an `[Unprotected]` read | It is a hint. The rule is enforced elsewhere |

## A validator with a read model

For a borrowing command, let the validator decide whether stock is available and let `Handle()` record the loan. `BookStock?` allows the validator to treat a missing stock model as no available copies, using the same business message.

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

Under `On`, Arc starts the command's unit of work, the transaction that groups its reads and writes, and enables read protection before the filters run. It constructs the validator inside that unit. Chronicle captures the event position, folds `BookStock` by applying its events, and registers the guard before the validator sees the value.

If the validator approves, `Handle()` returns `BookBorrowed`. At commit, Chronicle checks whether a `CopyAdded` or `BookBorrowed` for this book arrived after the captured position. If so, nothing is written. The caller receives HTTP `400` with the reason `concurrencyViolation` and the detail `ReadChanged`.

If `Handle()` also takes the same model for the same key, it shares the validator's instance and guard. The model is folded once per execution, whether the two take it as a plain parameter or as `DecisionRead<T>`.

When the validator rejects, nothing is written and Arc never retries the rejection. Its message reaches the client through the ordinary validation result. Use a nullable parameter when absence is a business condition, as it is here, and a non-nullable parameter when the command cannot run without the model. See [Nullable means you handle absence](../read-models/injecting-into-commands.md#nullable-means-you-handle-absence).

## Caller severity cannot skip errors under declared On

HTTP caller severity is capped at `Warning` by default. Even when a command or host opts in to relaxing errors, declared read protection `On` supplies an `Error` blocking floor unless the command declares `[BlockOnValidationSeverity(...)]`. Validator errors and `Unknown` results block even when the caller sends `X-Allowed-Severity: 3`; warnings and information remain caller-filterable. The floor also applies to `Provide()` control results. A `Result<TEvent, ValidationResult>` error returned by `Handle()` remains a rejection.

The command's read-protection attribute takes precedence over its own assembly's attribute. Lowering the operational `ReadProtection:MaximumMode` does not remove this declared floor, although it can remove the read's guard. An explicit blocking-severity attribute takes precedence. Under `Off` or `Measure`, callers can skip validator errors unless you declare `[BlockOnValidationSeverity(ValidationResultSeverity.Error)]`. See [Validation severity filtering](../../commands/validation-severity-filtering.md).

## A validator for one stream

A plain constructor parameter whose model declares `[EventStream<TSource>(stream)]` reads the command's own stream, including its resolved stream-id template. Under `On`, validator and `Handle()` share one enrolled fold. Under `Off` or `Measure`, the fold stays stream-scoped but is unguarded. A missing template part returns `400` naming the property before any read; a mismatched route is a configuration error in every mode. Do not use `[LookUpBy]` to redirect a per-stream parameter.

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | [Per-stream model and validator](./scenarios/per-stream.md#a-monthly-limit) |
| The entity's whole history, such as a balance | Whole-source model. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Rich behaviour across the whole entity | Aggregate, without reads of the same source |
| Another month or another source | `IDecisionReads` stream overload directly in `Handle()`, with a reasoned ARCCHR0040 suppression linked to the tracking feature |
| Uniqueness across sources | Constraint |

A validator runs before `Provide()`. Do not also acquire a later stream decision for the same source: its boundary may not cover the validator's earlier read. [ARCCHR0036](../code-analysis/ARCCHR0036.md) warns about that combination.

## Several keys in one validator

Put `[LookUpBy(nameof(Command.Property))]` on each constructor parameter. The name is resolved on the command, not on the validator. Each read gets its own guard, and `Handle()` can return one `EventForEventSourceId` for each entity without implementing `ICanProvideEventSourceId`. See [Two entities](./scenarios/two-entities.md) and [the transfer example](./reads.md#choose-the-key-with-lookupby).

For a read keyed by the selected caller, use a caller-keyed `Handle` parameter instead: `[LookUpBy<CallerSubject>]` reads the exact `sub` claim, and a custom `CallerClaim` can name an application claim. The provider receives only the captured caller and target model type, not services. An absent or ambiguous key is an authorization refusal, including during validation preflight; it is not nullable model absence. See [caller-derived keys](../read-models/injecting-into-commands.md#key-a-read-by-the-caller). The key selection itself enables no guard: choose `On` when the rule must hold under concurrency. Neither lookup form may redirect a per-stream model.

## What a validator may and may not do under On

| In a validator | Under `On` |
| --- | --- |
| A read-model constructor parameter | Protected |
| `[Unprotected("why")]` on the parameter or the model | Advisory. It guards nothing |
| `IReadModels`, `IEventStore` or `IEventLog` used in a rule | Refused. Take the model as a parameter instead |
| `IMongoCollection<T>` of a Chronicle-backed model | Refused. Use `[Unique]` for uniqueness |
| A service such as a price list | Unknown. It is outside the guard |
| A validator registered by hand as scoped, singleton, a factory or an instance | Refused. Let convention discovery register it, which makes it transient |

Under `On`, Arc builds a fresh validator for each command invocation so its read-model parameters belong to that command's unit of work. A scoped, singleton, factory or instance registration could supply a validator built for a different invocation, so Arc refuses it with this error:

```text
Discoverable validator 'BorrowBookValidator' cannot run while read protection is On because Arc did not construct it for this invocation. Let convention discovery register it as transient; do not use a scoped, singleton, factory or instance registration.
```

Arc checks the registration the container actually uses, which is the last one. Registering a validator again by hand after `AddCratisArc()` replaces the convention registration, so that validator is refused under `On` unless the new registration is again a plain transient binding of the validator to itself.

Other constructor parameters, such as `ILogger<T>`, are resolved from the container.

## The validate endpoint

`{url}/validate` runs the filters and validators without calling `Provide()` or `Handle()`, and without committing. Under `On`, the reads are fresh, but nothing is guarded or written because nothing commits. A successful validation is feedback for the form, not a promise that execution will succeed.

Calling `/validate` and then executing under `On` folds the model twice, once per request. Execution must read its own state and guard from that position; it cannot reuse a fold from the earlier validation request. Within the execution, validators and `Handle()` share a single fold.

The user can therefore see "no copies left" while filling in the form, and execution checks the same rule again against a fresh, guarded read.

## Under Off and Measure

Under `Off`, a plain read-model parameter supplies state without a concurrency guard. The validator can reject based on what it reads, but approval alone cannot hold the rule until commit:

1. Two members submit at the same time. Both validators read `Available: 1`.
2. Both validators pass. Both `Handle()` methods return `BookBorrowed`.
3. Both events are appended.

If you keep a rule over a plain read, decide what actually holds it: a constraint, an aggregate, a stream decision, an explicit scope, or a repair you accept. Mark the command's read `[Unprotected("why")]` to record that choice. [ARCCHR0011](../code-analysis/ARCCHR0011.md) reports plain reads in event-producing commands under `Off`.

`Measure` keeps the validator's `Off` behavior and reports what `On` would do. The report helps you plan the change; it does not protect the read.

### Commands marked [ProtectedDecision]

By default, a `[ProtectedDecision]` command outside `On` runs a discoverable validator only if it has exactly one public, parameterless constructor. To take `DecisionRead<T>` or the command-aware `IDecisionReads` instead, opt into [certified validator construction](./certified-validators.md). That opt-in requires an attested native Microsoft DI scope and an inspectable, fresh transient dependency graph. It supports an Arc-owned configuration callback, not arbitrary factories or prebuilt instances, and is available only on untrimmed JIT runtimes.

Under `On`, Arc can construct a validator with plain read-model parameters without that provider opt-in. When moving a command to `On`, put its state checks in the validator and replace `DecisionRead<T>` with a plain parameter.

### What Arc does not check

Construction checks do not prove that a validator's rules read only its declared parameters. Under `On`, Arc checks that it constructs the validator for this invocation, but hidden state can still affect a rule. Keep these out of validators used by a protected command:

- static members and service locators read by a rule;
- property injection, or code that resolves the validator and sets a field a rule reads;
- a decorator that returns the inner instance after changing its state;
- `ApplyCondition` on an existing rule, a changed cascade mode, or rules added to a nested child validator.

Arc does not certify the logic of custom `ICommandFilter` implementations. Reads through Chronicle's guarded APIs are still subject to the [read refusals](./reads.md#what-each-read-becomes), but that does not cover hidden dependencies. Keep decision-state checks in a validator or `Handle()`, not in a custom filter.

## Where to go next

Use [Reads](./reads.md) to check whether your model can be protected, then choose a worked [scenario](./scenarios/index.md) for the rule you need. [Pitfalls](./pitfalls.md) covers mistakes that can leave a rule unenforced.

For validation behavior beyond read protection, continue with [Command validation](../../commands/validation.md). If you are deciding between a validator, aggregate or constraint, read [Choosing how a rule holds in Chronicle](/chronicle/dynamic-consistency-boundary/choosing/) before choosing the boundary.
