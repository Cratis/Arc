---
title: Pitfalls that break a rule
description: Silent and loud ways a command's rule fails to hold, the analyzer that reports each one, and the fix.
---

Use this page to check a command you wrote, or to find out why a rule did not hold. A **silent** failure leaves the command successful and the rule broken. A **loud** failure rejects the command or throws. Most dangerous pitfalls are silent, and several have no analyzer.

## Summary

| Pitfall | Failure | Reported by | Fix |
| --- | --- | --- | --- |
| [A uniqueness check instead of a constraint](#a-uniqueness-check-instead-of-a-constraint) | Silent | Nothing | Constraint |
| [A plain read next to a protected decision](#a-plain-read-next-to-a-protected-decision) | Silent | ARCCHR0011 (Info) | `DecisionRead<T>` |
| [An immediate append in a protected command](#an-immediate-append-in-a-protected-command) | Silent: partial commit | ARCCHR0007 (Warning), ARCCHR0012 (Info) | Return the events |
| [An aggregate commit whose result is ignored](#an-aggregate-commit-whose-result-is-ignored) | Silent | Nothing | Return `Commit()` |
| [No event source id, or two candidates](#no-event-source-id-or-two-candidates) | Silent | ARCCHR0002 (Warning) for two candidates | `EventSourceId` or `ICanProvideEventSourceId` |
| [A decision-read model without an Id](#a-decision-read-model-without-an-id) | Loud: error response when the command runs | Nothing at build time | An `Id` property |
| [A model that folds a different key than the facts are written to](#the-model-folds-a-different-key-than-the-one-the-facts-are-written-to) | Silent | Nothing | Write the facts to the read key |
| [`concurrency: true` as a guard](#concurrency-true-is-not-the-guard) | Silent | ARCCHR0016 (Warning) only with exact scopes; ARCCHR0011 (Info) for a plain read | A real guard |
| [A decision read on the wrong kind of command](#a-decision-read-on-the-wrong-kind-of-command) | Loud when unmarked, silent when `[Unprotected]` | Nothing | `[ProtectedDecision]` |
| [Results you must check](#results-you-must-check-when-you-use-chronicle-directly) | Silent | Nothing | Check `IsSuccess`, commit before you respond |
| [A validator with constructor dependencies](./validators.md#what-a-protected-command-accepts) | Loud: HTTP 500 | Nothing at build time; ARCCHR0011 (Info) points the way | Parameterless validator, or move the rule |
| [The first append into a scope](#the-first-append-into-a-scope) | Silent | Nothing | Opt in |
| [Where the guard stops](#where-the-guard-stops) | Silent | Nothing | Outside the guarantee |
| [Tests without decision mode](#tests-without-decision-mode) | Silent | Nothing | `UseDecisionReads()` |

Two failures are loud by design. A conflict rejects the command with the reason `concurrencyViolation` and is never retried; the caller reads again and resubmits. A read shape that decision reads refuse fails the command each time it runs; `DecisionReadAdmissions` in your specs finds it before production (see [protected decision scenarios](../../testing/chronicle.md#check-that-chronicle-admits-the-decision-reads)).

All examples on this page share one namespace.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CopyAdded();

[EventType]
public record BookBorrowed(Guid MemberId);

[EventType]
public record BookReturned();

[EventType]
public record LoanTaken(string Isbn);

[EventType]
public record LoanReturned(string Isbn);

[EventType]
public record MemberRegistered(string Email);

public record BookStock(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookBorrowed>, Increment<BookReturned>] int Available);

public record MemberLoans(
    EventSourceId Id,
    [Increment<LoanTaken>, Decrement<LoanReturned>] int Active);
```

## A uniqueness check instead of a constraint

A check followed by an append leaves a gap. Two requests both see "not taken" and both append.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Monads;

public interface IMemberDirectory
{
    Task<bool> EmailExists(string email);
}

[Command]
public record RegisterCheckedMember(string Email)
{
    public async Task<Result<MemberRegistered, ValidationResult>> Handle(IMemberDirectory directory) =>
        await directory.EmailExists(Email)
            ? ValidationResult.Error("A member with this email address is already registered.")
            : new MemberRegistered(Email);
}
```

Nothing reports it, and it works in every test that runs one request at a time. Use a [constraint](./scenarios.md#a-unique-value). The same holds for a validator that asks the question.

## A plain read next to a protected decision

A protected command guards the decision reads it takes. A plain read in the same command is advisory, and the difference is one type name:

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Monads;

[Command]
[ProtectedDecision]
public record BorrowBookPlainRead(EventSourceId BookId, Guid MemberId)
{
    public Result<BookBorrowed, ValidationResult> Handle(BookStock stock) =>
        stock.Available <= 0
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(MemberId);
}
```

The command is marked, it compiles and it runs, and it guards nothing: `BookStock` is a plain read, so two members can both see `Available: 1`. Arc refuses nothing. It reports the parameter as [ARCCHR0011](../code-analysis/index.md#arcchr0011-unguarded-legacy-decision-reads) at Info severity. [ARC0006](../../code-analysis/ARC0006.md) warns on a non-nullable parameter only for types marked `[ReadModel]` or targeted by an `IProjectionFor<T>` in the same compilation, so it does not report this model-bound `BookStock`.

Take every state the rule depends on as a `DecisionRead<T>`, or as `IDecisionReads.Get<T>` for another key. Raise ARCCHR0011 to a warning in projects that rely on protected decisions:

```ini
[*.cs]
dotnet_diagnostic.ARCCHR0011.severity = warning
```

## An immediate append in a protected command

`IEventLog.Append` writes at once, before the owner commits and ignoring the decision read. If the commit then conflicts, the immediate event stays.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventType]
public record BorrowingRecorded(EventSourceId BookId);

[Command]
[ProtectedDecision]
public record BorrowBookImmediate(EventSourceId BookId, Guid MemberId)
{
    public async Task<Result<BookBorrowed, ValidationResult>> Handle(
        DecisionRead<BookStock> stock,
        IEventLog eventLog)
    {
        if (stock.Instance is not { Available: > 0 })
        {
            return ValidationResult.Error("No copies of this book are available.");
        }

        await eventLog.Append(MemberId.ToString("D"), new BorrowingRecorded(BookId));
        return new BookBorrowed(MemberId);
    }
}
```

Arc reports [ARCCHR0007](../code-analysis/index.md#arcchr0007-command-handler-injects-ieventlog) as a warning on the `IEventLog` parameter and [ARCCHR0012](../code-analysis/index.md#arcchr0012-immediate-append-with-a-decision-read) as Info on the call. A successful immediate append counts as committed, so the command ends with a partially committed state. Return the events, or stage them with `eventLog.Transactional.Append(...)`, as in the [scenarios](./scenarios.md#an-injected-event-log).

## An aggregate commit whose result is ignored

An aggregate's `Failed(...)` accumulates errors, and only `Commit()` turns them into a result.

```csharp
using Cratis.Arc.Chronicle.Aggregates;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[EventType]
public record CopyLent(Guid MemberId);

public class Copy : AggregateRoot
{
    bool _lent;

    public async Task Lend(Guid memberId)
    {
        if (_lent)
        {
            Failed("The copy is already lent.");
            return;
        }

        await Apply(new CopyLent(memberId));
    }

    public void OnCopyLent(CopyLent @event) => _lent = true;
}

[Command]
public record LendCopyIgnoringTheResult(EventSourceId CopyId, Guid MemberId)
{
    public async Task Handle(Copy copy)
    {
        await copy.Lend(MemberId);
        await copy.Commit();
    }
}

[Command]
public record LendCopyReturningTheResult(EventSourceId CopyId, Guid MemberId)
{
    public async Task<AggregateRootCommitResult> Handle(Copy copy)
    {
        await copy.Lend(MemberId);
        return await copy.Commit();
    }
}
```

The first command ends successfully even when the copy was already lent: the failure is in the result that nothing reads. The second returns it, and Arc turns it into the command's outcome. The same silent success follows if an aggregate applies an event and then calls `Failed(...)`: the applied event is already staged, and automatic completion does not read the failure list.

`Commit()` is also the last staging step. It commits the whole command unit, so do not stage anything afterwards. Do not combine an aggregate with `[ProtectedDecision]` reads: `Commit()` throws once a decision read is enrolled. For aggregate events and returned events on one event source, the ordered-batch path throws and the older `AddEvent` path silently replaces the earlier scope; choose one way to write.

## No event source id, or two candidates

A command with no event source id property gets a new random id on every call, so nothing can ever conflict. A raw `Guid` is not an event source id:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[EventType]
public record PaymentRecorded(decimal Amount);

[Command]
public record RecordPaymentToRandomStream(Guid AccountId, decimal Amount)
{
    public PaymentRecorded Handle() => new(Amount);
}
```

Declare the id as `EventSourceId` (or an `EventSourceId<T>`-derived type) so the event goes to the account's stream. When a command has two such properties, Arc cannot choose and [ARCCHR0002](../code-analysis/index.md#arcchr0002-ambiguous-command-identity) warns:

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;

[EventType]
public record LoanRecorded(EventSourceId Member, EventSourceId Book);

[Command]
public record RecordLoanWithTwoKeys(EventSourceId Member, EventSourceId Book)
{
    public LoanRecorded Handle() => new(Member, Book);
}

[Command]
public record RecordLoanWithAKey(EventSourceId Member, EventSourceId Book) : ICanProvideEventSourceId
{
    public EventSourceId GetEventSourceId() => Member;

    public LoanRecorded Handle() => new(Member, Book);
}
```

The second command is the fix: `ICanProvideEventSourceId` names the stream. The key also decides which instance a `DecisionRead<T>` reads, so the choice is part of the rule. See [resolving the event source id](../resolving-event-source-id.md).

## A decision-read model without an Id

A decision read matches the command's key to the model's `Id` property, which must be a string-like or `Guid` property.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

public record BookStockWithoutId(
    string Isbn,
    [Increment<CopyAdded>, Decrement<BookBorrowed>] int Available);

[Command]
[ProtectedDecision]
public record BorrowBookWithoutId(EventSourceId BookId, Guid MemberId)
{
    public Result<BookBorrowed, ValidationResult> Handle(DecisionRead<BookStockWithoutId> stock) =>
        stock.Instance is not { Available: > 0 }
            ? ValidationResult.Error("No copies of this book are available.")
            : new BookBorrowed(MemberId);
}
```

This compiles. When the command runs, Chronicle refuses the model with the reason `KeyConversion`, and the command fails with `DecisionReadCouldNotBeAcquired` every time. The same happens for a reducer, a join, a child collection, `FromEvery`, a key from an event property, or a projection with no event types. Rename the key to `Id`, or use a stream decision, an aggregate or a constraint. Add `DecisionReadAdmissions.ShouldAdmitDecisionReadsIn(...)` to your specs.

## The model folds a different key than the one the facts are written to

A decision read folds the events of its key. Events written to another event source are invisible to it.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
[ProtectedDecision]
public record TakeLoanOnItsOwnStream(EventSourceId Member, string Isbn)
{
    public Result<EventForEventSourceId, ValidationResult> Handle(DecisionRead<MemberLoans> loans) =>
        loans.Instance is { Active: >= 3 }
            ? ValidationResult.Error("The member has reached the loan limit.")
            : new EventForEventSourceId(EventSourceId.New(), new LoanTaken(Isbn));
}
```

`MemberLoans` counts `LoanTaken` on the member's stream, but each loan is written to a new stream. The count is always zero, the limit never triggers, and the guard watches a stream no one writes to. Write the facts to the key you read (`return new LoanTaken(Isbn)` goes to `Member`), or model the rule on one key that carries all of them. A rule over keys that can join the set later, such as loans that each live on a book stream, is not guarded: a new key is in nobody's read set.

## `concurrency: true` is not the guard

`concurrency: true` on `[EventSourceType]`, `[EventStreamType]` and `[EventStreamId]` narrows the check that Arc builds for returned events. That check reads the tail after `Handle()`, so it cannot bind your decision to what you read.

```csharp
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
[EventSourceType("Book", concurrency: true)]
public record BorrowBookNarrowed(EventSourceId BookId, Guid MemberId)
{
    public async Task<Result<BookBorrowed, ValidationResult>> Handle(IReadModels readModels)
    {
        var stock = await readModels.GetInstanceById<BookStock>(BookId);
        return stock is { Available: > 0 }
            ? new BookBorrowed(MemberId)
            : ValidationResult.Error("No copies are available.");
    }
}
```

The plain `IReadModels` read is reported by [ARCCHR0011](../code-analysis/index.md#arcchr0011-unguarded-legacy-decision-reads) at Info severity; nothing reports the belief that the flag protects the decision. Use `concurrency: true` to choose which dimensions bound the default check, not to protect a decision. [ARCCHR0016](../code-analysis/ARCCHR0016.md) reports the flag only when the same command also returns exact scopes, where it has no effect.

## A decision read on the wrong kind of command

| Command | `DecisionRead<T>` behaviour | Failure |
| --- | --- | --- |
| `[ProtectedDecision]` | Guarded read | None |
| `[Unprotected]` | An unguarded read, shared within the invocation | Silent |
| Unmarked | Refused with `ProtectedDecisionReadRequiresProtectedCommand` | Loud |

Mark the command `[ProtectedDecision]` before you take `DecisionRead<T>`. A command with both attributes runs as protected and then throws `CommandCannotBeBothProtectedAndUnprotected`.

## Results you must check when you use Chronicle directly

Outside a command, nothing checks results for you.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.AspNetCore.Http;
using ScopeBuilder = Cratis.Chronicle.EventSequences.Concurrency.ConcurrencyScopeBuilder;

[EventType]
public record Withdrawn(decimal Amount);

public static class DirectAppends
{
    public static async Task<IResult> IgnoresTheResult(IEventLog eventLog, EventSourceId accountId)
    {
        await eventLog.Append(accountId, new Withdrawn(10m));
        return Results.Ok();
    }

    public static async Task<bool> BuildsAnIncompleteScope(IEventLog eventLog, EventSourceId accountId)
    {
        var scope = new ScopeBuilder().WithEventSourceId(accountId).Build();
        var result = await eventLog.Append(accountId, new Withdrawn(10m), concurrencyScope: scope);
        return result.ConcurrencyCheckPerformed;
    }
}
```

- `Append` returns an `AppendResult`: a constraint violation or a concurrency violation is a value, not an exception. `IgnoresTheResult` reports success whatever happened. Check `IsSuccess`.
- A scope with no sequence number and no `ExpectingNoMatchingEvent()` is skipped by the kernel, and `ConcurrencyCheckPerformed` is `false`. Set a sequence number, or use `ExpectingNoMatchingEvent()`.
- With `IDecisionReads.Get<T>` and a unit of work, commit with `IUnitOfWorkCompletionFeature.CommitAsync()` before you respond. The request middleware commits only after the endpoint has returned, and it reports no concurrency violation to the caller.
- A plain `Append` after `Get<T>` in the same request is written immediately and bypasses the read's guard. Use `Transactional.Append`.
- In an Arc project, qualify Chronicle's `ConcurrencyScopeBuilder`: `Cratis.Arc.Chronicle.Commands` has a static class with the same name.

The [Level 3 scenarios](./scenarios.md#level-3-chronicle-used-directly-in-an-arc-app) show the correct shapes.

## The first append into a scope

The first event on a new event source, or the first with a new event source type, stream type or stream id, has no tail to compare against. By default Chronicle skips the check, so two concurrent creations can both succeed. Opt in application-wide, or ask for it per append with `ExpectingNoMatchingEvent()`:

```csharp
using Cratis.Chronicle;

public static class FirstAppendCheck
{
    public static void Configure(ChronicleOptions options) =>
        options.ConcurrencyOptions.CheckFirstAppendIntoAScope = true;
}
```

The option rejects appends that succeed today, and its default is planned to change in the next major version. Read [concurrency in Chronicle](/chronicle/events/concurrency/) before you turn it on. A new aggregate and a protected decision read already check their first append.

## Where the guard stops

The guard covers the events in the read set. It does not cover:

- `Revise` and `Redact` between the read and the append;
- a projection definition changed by another process, until this client registers again;
- other event sequences and external or non-Chronicle state;
- a unique-constraint index update that fails after the append is durable: Chronicle logs it, a later duplicate can pass, and a reindex repairs it;
- constraints across namespaces: tenants in different namespaces can claim the same value.

Do not design a rule that relies on any of these.

The guard also does not protect a writer that decides without one. A guard detects every matching event appended after its boundary, whoever wrote it. It only protects decisions that carry a guard: a writer that decides without one can still break the rule. The rule holds only when every writer that decides on it is guarded, or when it is a constraint, which the kernel checks for every append.

## Tests without decision mode

A scenario without `UseDecisionReads()` does not exercise decision reads or concurrent writers. Pinned read models cannot supply a guard, and `AppendConcurrently` throws. Seed real events and queue the competing event; see [protected decision scenarios](../../testing/chronicle.md#protected-decision-scenarios).

## Where to go next

- [Choosing how a command's rule holds](./index.md): the decision matrix.
- [Scenarios](./scenarios.md): the correct shape for each rule.
- [Code analysis](../code-analysis/index.md): every `ARCCHR` rule.
