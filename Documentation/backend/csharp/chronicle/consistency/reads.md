---
title: Reads under read protection
description: Which reads a command can make under On, which are protected, advisory, refused or outside the guarantee, and how to declare an advisory read or choose the key a parameter reads.
---

Use this page when you want to know what a read in a command becomes under `On`, how to mark one as advisory, or how to choose which key a parameter reads. Under `Off` none of this applies: every read is a plain read.

## Four classes of read

| Class | What it is | Under `On` |
| --- | --- | --- |
| **Protected** | A read model parameter, or `IDecisionReads.Get` in `Provide()`, that Chronicle can guard | Read fresh, and enrolled as a guard before your code sees the value |
| **Advisory** | A read you marked `[Unprotected("why")]` | A stored read that guards nothing. The reason goes to telemetry |
| **Refused** | Any other read Arc or Chronicle can see | The command fails with a `500` that names the read and the fix |
| **Unknown** | A dependency Arc cannot see: a service, another database, an HTTP call | Outside the guarantee. Never reported as protected |

## What each read becomes

| What you write in a command | Under `On` |
| --- | --- |
| A read-model parameter backed by a source-keyed projection (materialized, passive or model-bound) | **Protected.** The fold is read from the event log, not from the read model store |
| A read-model parameter backed by an unfiltered reducer or self-reducing model with no constructor dependencies other than `ILogger<T>` | **Protected** |
| A reducer with a filter or other constructor dependencies | **Refused** |
| A model with joins, children, `FromEvery`, `FromEventProperty` or `UsingKey` | **Refused** |
| A projection keyed by a stream | **Refused** as a plain parameter. Read it with `IDecisionReads.Get` and an explicit stream in `Provide()` |
| A key that is not a string or a `Guid`; an ambiguous projection; a model of another event sequence; a definition that does not match the registered one | **Refused** |
| A projection with Pinned generation delivery | **Refused** |
| A model with no instance yet | **Protected.** The parameter is `null` and the guard watches for the first event |
| `DecisionRead<T>` with `[ProtectedDecision]` | Unchanged. Redundant under `On` |
| `IDecisionReads.Get<T>` in `Provide()` | **Protected** |
| `IDecisionReads.GetDetached<T>` | **Refused** unless inside an advisory method |
| `IReadModels`, raw `IEventStore`, `IEventLog` or `IEventSequence` reads | **Refused** at the call |
| `IMongoCollection<T>` of a Chronicle-backed model | **Refused** when the parameter is resolved |
| `IEventLog.Append` (an immediate append), anywhere in the invocation | **Refused**. `IEventLog.Transactional.Append` stages the event and is allowed |
| An aggregate and a plain read of the same source | **Refused**. A plain read of a different source is fine |
| A service, another database, HTTP | **Unknown** |

Reads inside a validator constructor follow the same rules. See [Validators](./validators.md).

[Refusals](./refusals.md) lists each refusal with its message and fix. [How it works](./how-it-works.md) explains what a protected read guards.

## Declare an advisory read

`[Unprotected("why")]` is the opt-out. Write the reason as a sentence a reviewer can judge. The reason goes to telemetry as `advisory_read`.

| Place | Covers | Notes |
| --- | --- | --- |
| A parameter | That parameter | The common case: context you read but do not decide from |
| A read-model type | Every parameter of that type, in every command | Use for reference data you accept as stale |
| A `Handle()` or `Provide()` method | **The method body only**: calls to `IReadModels` and other raw reads inside it | It does not cover the method's parameters. [ARCCHR0034](../code-analysis/ARCCHR0034.md) reports a method-level declaration beside read-model parameters |
| A command | The whole command runs as `Off` | Use it for one command you cannot move yet |

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

public record Isbn(string Value) : EventSourceId(Value);

[EventType]
public record BookSold(decimal Price);

public record BookStock(EventSourceId Id, [Increment<BookSold>] int Sold);

[FromEvent<BookSold>]
public record BookPrice(EventSourceId Id, decimal Price);

[Command]
public record SellBook(Isbn Isbn)
{
    public BookSold Handle(
        BookStock stock,
        [Unprotected("shown to the seller as context; the price is copied as of this read")] BookPrice price) =>
        new(price.Price);
}
```

Here `stock` is protected and `price` is not. The guard of this command covers `BookSold` for the book and nothing else.

Do not use `[Unprotected]` to silence a refusal you do not understand. A refusal means Chronicle could not guard that read exactly. Marking it advisory makes the rule depend on a stale value again.

A command cannot declare both `[ProtectedDecision]` and `[Unprotected]`, and cannot combine a command-level `[ReadProtection(ReadProtectionMode.On)]` or `Measure` with a command-level `[Unprotected]`. Arc fails the command with `CommandCannotBeBothProtectedAndUnprotected` or `ConflictingReadProtection`. [ARCCHR0029](../code-analysis/ARCCHR0029.md) reports both at build time.

## Choose the key with `[LookUpBy]`

A read-model parameter reads the instance whose `Id` equals a key from the command. `[LookUpBy(nameof(Property))]` on the parameter names the command property that supplies it. It works in every mode.

| Mode | Without `[LookUpBy]` |
| --- | --- |
| `Off`, `Measure` | The command's event source id |
| `On` | The one command property whose type equals the model's `Id` type. If several match, `AmbiguousReadModelLookup`. If none matches, the command's event source id |

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Monads;

public record AccountId(string Value) : EventSourceId(Value);

[EventType]
public record Deposited(decimal Amount);

[EventType]
public record Withdrawn(decimal Amount);

public record AccountBalance(
    AccountId Id,
    [Increment<Deposited>, Decrement<Withdrawn>] decimal Balance);

[Command]
public record Transfer(AccountId From, AccountId To, decimal Amount) : ICanProvideEventSourceId
{
    const decimal MaximumBalance = 1_000_000m;

    public EventSourceId GetEventSourceId() => From;

    public Result<IEnumerable<EventForEventSourceId>, ValidationResult> Handle(
        [LookUpBy(nameof(From))] AccountBalance? source,
        [LookUpBy(nameof(To))] AccountBalance? target)
    {
        if (source is null || source.Balance < Amount)
        {
            return ValidationResult.Error("The source account has insufficient funds.");
        }

        if (target is not null && target.Balance + Amount > MaximumBalance)
        {
            return ValidationResult.Error("The target account cannot hold that much.");
        }

        return new[]
        {
            new EventForEventSourceId(From, new Withdrawn(Amount)),
            new EventForEventSourceId(To, new Deposited(Amount))
        };
    }
}
```

Each parameter is its own guard: one on the event source `From`, one on the event source `To`. Without the attributes, `Transfer` has two properties of the model's `Id` type, and `AmbiguousReadModelLookup` refuses it ([ARCCHR0023](../code-analysis/ARCCHR0023.md) reports it at build time).

`[LookUpBy]` names a public property of the command, and the property's type must be the type of the model's `Id`. A name that does not exist, or a parameter that is not a read model, is `InvalidLookUpBy`, and [ARCCHR0031](../code-analysis/ARCCHR0031.md) reports it, and a property of another type, at build time.

## The model must be keyed by the entity you append to

A guard covers the events of one event source. A model keyed by an event property, for example `UsingKey(e => e.Member)` over events that are appended to the book, would read facts that live on another source. Chronicle refuses it, because a conflict on the member's events cannot be detected from the book.

The fix is to append a fact to the entity the model is keyed by. To count a member's loans, append `LoanTaken` to the member as well as `BookBorrowed` to the book, in one batch. See [two entities](./scenarios/two-entities.md).

## Where to go next

- [Refusals](./refusals.md)
- [Read models in commands](../read-models/injecting-into-commands.md): nullability, `Provide()` and the plain-read reference.
- [Chronicle decision reads](/chronicle/read-models/decision-reads/): which projection shapes are admitted.
