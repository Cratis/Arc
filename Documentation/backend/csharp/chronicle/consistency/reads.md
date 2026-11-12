---
title: Cratis Arc protected command reads
description: "Under On, Cratis Arc guards supported command reads with Chronicle. Choose a read key, declare advisory reads, and understand what stays unguarded."
---

Under read protection `On`, Cratis Arc resolves supported read-model parameters as fresh reads guarded by Cratis Chronicle. Chronicle folds the model by applying its events, then checks for relevant changes before commit. If those events changed after the read, the command is rejected instead of writing from an out-of-date decision.

`Off` leaves plain read-model parameters unguarded. `Measure` keeps those reads unguarded while reporting what `On` would protect or refuse. Explicit `[ProtectedDecision]` commands with `DecisionRead<T>` still guard their declared decision reads outside `On`.

Start with a plain read-model `Handle()` parameter. Use `[LookUpBy]` when a command property supplies its key, or a per-stream model when the rule concerns the command's stream route. If supported parameters cannot express the key or stream id, take `IDecisionReads` (or `DecisionRead<T>`) directly in `Handle()`. That path needs an ARCCHR0040 suppression with a reason and a link to the tracking feature; see the [stream example](../read-models/injecting-into-commands.md#read-a-projection-within-one-stream).

## Four classes of read

A read is protected only when Chronicle can guard the facts it supplies to the command. Marking a read advisory accepts that it may be stale; it does not make an unsupported read safe for a decision.

| Class | What it is | Under `On` |
| --- | --- | --- |
| **Protected** | A read-model parameter, or an explicit computed-key read in `Handle()`, that Chronicle can guard | Read fresh, with a guard registered before your code sees the value |
| **Advisory** | A read you marked `[Unprotected("why")]` | A stored read that guards nothing. The reason goes to telemetry |
| **Refused** | Any other read Arc or Chronicle can see | The command fails with a `500` that names the read and the fix |
| **Unknown** | A dependency Arc cannot see: a service, another database, an HTTP call | Outside the guard. Never reported as protected |

## What each read becomes

| What you write in a command | Under `On` |
| --- | --- |
| A read-model parameter backed by a source-keyed projection (materialized, passive or model-bound) | **Protected.** The fold is read from the event log, not from the read model store |
| A read-model parameter backed by an unfiltered reducer with no constructor dependencies other than `ILogger<T>` (self-reducing models are exempt from that constructor restriction) | **Protected** |
| A reducer with a filter or other constructor dependencies | **Refused** |
| A model with joins, children, `FromEvery`, `FromEventProperty` or `UsingKey` | **Refused** |
| A declared per-stream model | **Protected** on the command route. A projection keyed by a stream without that declaration needs an explicit stream read in `Handle()` with a reasoned ARCCHR0040 suppression linked to the tracking feature |
| A key that is not a string or a `Guid`; an ambiguous projection; a model of another event sequence; a definition that does not match the registered one | **Refused** |
| A projection with Pinned generation delivery | **Refused** |
| A model with no instance yet | **Protected.** A nullable parameter receives `null` and the guard watches for the first event. A required parameter rejects with a validation result |
| `IDecisionReads.Get<T>` in `Handle()` for a key no parameter can express, with a reasoned ARCCHR0040 suppression linked to the tracking feature | **Protected** |
| Existing advanced `DecisionRead<T>` with `[ProtectedDecision]` | Still supported. Prefer a plain parameter under `On` |
| `IDecisionReads.GetDetached<T>` | **Refused** unless inside an advisory method |
| `IReadModels`, raw `IEventStore`, `IEventLog` or `IEventSequence` reads, including inside a command filter | **Refused** at the call |
| A read model injected into the constructor of a command filter or a service | **Refused** with `ReadModelInjectedOutsideDecision`, unless the read-model type is `[Unprotected("why")]`. Move the rule into `Handle()` or a validator. A model declared for one event stream is bound to the command's route, as for a parameter |
| `IMongoCollection<T>` of a Chronicle-backed model | **Refused** when the parameter is resolved |
| `IEventLog.Append` (an immediate append), anywhere in the invocation | **Refused**. `IEventLog.Transactional.Append` stages the event and is allowed |
| An aggregate and a plain read of the same source | **Refused**. A plain read of a different source is fine |
| A service, another database, HTTP | **Unknown** |

Validator constructor parameters follow the same read rules. Use a [validator](./validators.md) when the model supplies a state-dependent rule that must pass before `Handle()` runs.

If a read is refused, find its message and fix in [Refusals](./refusals.md). For the event boundary each guard covers, read [How it works](./how-it-works.md).

## Missing models and invalid keys

Under `On`, a missing required (non-nullable) protected read-model parameter rejects the command with HTTP `400` and the message `{ReadModel} '{key}' does not exist.`. `ReadModel` is the model's simple CLR type name, matching its Screenplay view name. The validation result uses `ValidationResultReason.Rule` (`rule`), Arc's ordinary validation reason. A nullable parameter still receives `null` for a valid key with no instance.

A protected key is treated as missing if it is empty, whitespace-only, exactly `*`, contains `#`, or has leading or trailing whitespace. Arc checks the raw key and each composite key part before acquiring a read, even if an instance exists at that key. This rejection also applies to nullable protected parameters. Text keys appear unchanged in the message, including whitespace. For a composite record or tuple key, the diagnostic key text is empty (`Model '' does not exist.`), matching the Screenplay reference evaluator; concepts are unwrapped before checking their values.

These rules apply to protected reads, not advisory reads. `Off` and `Measure` retain their existing lookup, nullability, messages and validation reasons. Other read-protection refusals retain their existing outcomes.

## Require generated key metadata

Arc uses generated command property getters and read-model key types for command key lookup. Missing metadata is rejected by default with `CommandReadModelKeysMissing` instead of reflecting, so enable the source generator. Discovered commands running under `On` are checked at startup; command and model metadata used by lookup is also checked at resolution. Missing and invalid settings fail closed.

Set `ReadProtection:RequireGeneratedMetadata` to `false` to opt out and keep reflection compatibility on runtimes with dynamic code support. Arc then logs one warning per missing type, without key values, explaining how to enable the source generator. Runtimes where `RuntimeFeature.IsDynamicCodeSupported` is `false` always require generated metadata; this does not make all of Arc NativeAOT-compatible.

This setting is separate from `ProtectedReadModelKeyOptions.Strict`, which requires accessor coverage for composite key parts. Neither setting changes which property or key a lookup selects.

## Declare an advisory read

Use `[Unprotected("why")]` for context the command may accept as stale, not for state that must hold when it commits. Write a reason that lets a reviewer judge that choice. Arc records it in telemetry as `advisory_read`.

| Place | Covers | Notes |
| --- | --- | --- |
| A parameter | That parameter | The common case: context you read but do not decide from |
| A read-model type | Every parameter of that type, in every command, and the type injected into a command filter or a service | Use for reference data you accept as stale |
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

In `SellBook`, `stock` is protected and `price` is advisory. The command copies the price seen by this read, accepting that it may change before commit. Only `BookSold` events for this book are covered by the command's guard.

Before marking a refused read advisory, find out why Chronicle cannot guard it. If the command relies on that value to decide whether it may proceed, opting out leaves the rule dependent on a potentially stale value.

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
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.ModelBound;
using FluentValidation;

public record AccountId(string Value) : EventSourceId(Value);

[EventType]
public record Deposited(decimal Amount);

[EventType]
public record Withdrawn(decimal Amount);

public record AccountBalance(
    AccountId Id,
    [Increment<Deposited>, Decrement<Withdrawn>] decimal Balance);

[Command]
public record Transfer(AccountId From, AccountId To, decimal Amount)
{
    public IEnumerable<EventForEventSourceId> Handle() =>
    [
        new(From, new Withdrawn(Amount)),
        new(To, new Deposited(Amount))
    ];
}

public class TransferValidator : CommandValidator<Transfer>
{
    public TransferValidator(
        [LookUpBy(nameof(Transfer.From))] AccountBalance? source,
        [LookUpBy(nameof(Transfer.To))] AccountBalance? target)
    {
        RuleFor(command => command.Amount)
            .Must(amount => source is not null && source.Balance >= amount)
            .WithMessage("The source account has insufficient funds.")
            .Must(amount => target is null || target.Balance + amount <= 1_000_000m)
            .WithMessage("The target account cannot hold that much.");
    }
}
```

Under `On`, the validator guards both accounts: `source` reads `From`, and `target` reads `To`. This lets it check the withdrawal and deposit against the state of the account each event will change. Without the attributes, `Transfer` has two properties of the model's `Id` type, so Arc refuses the ambiguous lookup with `AmbiguousReadModelLookup` ([ARCCHR0023](../code-analysis/ARCCHR0023.md) reports it at build time).

`[LookUpBy]` names a public property of the command, including on a validator constructor parameter. Arc converts the value to the wire key; the property's CLR type need not equal the model's `Id` type. Concepts are unwrapped, `Guid` values are formatted as identifiers, and `EventSourceId`-derived values use their underlying key. For example, a `ConceptAs<Guid>` property can select a model with a `Guid Id`, and the reverse works too. Other values use the runtime's string conversion. Protected reads validate raw keys before conversion as described in [Missing models and invalid keys](#missing-models-and-invalid-keys).

A name that does not exist, or a parameter that is not a read model, is `InvalidLookUpBy`; [ARCCHR0031](../code-analysis/ARCCHR0031.md) reports these declarations at build time. The exact-type convention in the table applies only when you omit `[LookUpBy]`.

## The model must be keyed by the entity you append to

A guard covers the events of one event source, the entity to which events are appended. The model's key must identify that source. For example, `UsingKey(e => e.Member)` over events appended to books groups a member's loans across book sources. A guard on the member's events cannot detect changes on those books, so Chronicle refuses the read.

To guard a member's loan count, append `LoanTaken` to the member as well as `BookBorrowed` to the book, in one batch. The member model can then read facts stored on the source its guard watches. Follow [two entities](./scenarios/two-entities.md) for that pattern.

## Where to go next

If a supported read still fails, use [Refusals](./refusals.md) to identify the cause. For parameter binding and nullability outside protection, continue with [Read models in commands](../read-models/injecting-into-commands.md), which also covers `Provide()`.

[Chronicle decision reads](/chronicle/read-models/decision-reads/) explains which projection shapes are admitted. If you generate a model document, [Protected reads in a generated Screenplay](../../generating-a-screenplay.md#protected-command-reads-and-esm-v25) shows how the declarations become `reads` and `require` in a `.play` document.
