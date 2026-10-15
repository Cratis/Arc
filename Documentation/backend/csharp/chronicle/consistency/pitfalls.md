---
title: Pitfalls that fail silently
description: The ways a rule stops holding without an error, with the analyzer that reports each and the fix.
---

Use this page when a rule holds in every test and still breaks in production. These failures do not raise an error. Failures that do raise one, such as a refused read or a conflict, are on [Refusals](./refusals.md).

## Summary

| Pitfall | Reported by | Fix |
| --- | --- | --- |
| [A uniqueness check instead of a constraint](#a-uniqueness-check-instead-of-a-constraint) | Nothing | Constraint |
| [A read that is not protected](#a-read-that-is-not-protected) | [ARCCHR0011](../code-analysis/ARCCHR0011.md) (Info, `Off`) | Turn read protection `On` |
| [An immediate append under Off](#an-immediate-append-under-off) | [ARCCHR0007](../code-analysis/ARCCHR0007.md) | Return the events |
| [An aggregate commit whose result is ignored](#an-aggregate-commit-whose-result-is-ignored) | Nothing | Return `Commit()` |
| [No event source id, or two candidates](#no-event-source-id-or-two-candidates) | [ARCCHR0002](../code-analysis/ARCCHR0002.md) for two candidates | `EventSourceId` or `ICanProvideEventSourceId` |
| [A model that folds a different key than the facts are written to](#the-model-folds-a-different-key-than-the-one-the-facts-are-written-to) | Nothing | Write the facts to the key you read |
| [`concurrency: true` as a guard](#concurrency-true-is-not-the-guard) | [ARCCHR0016](../code-analysis/ARCCHR0016.md) only with exact scopes | A real guard |
| [An advisory reason that stopped being true](#an-advisory-reason-that-stopped-being-true) | [ARCCHR0032](../code-analysis/ARCCHR0032.md) for inert declarations | Review `[Unprotected]` |
| [A lowered mode](#a-lowered-or-measured-mode) | A startup Warning | Remove the cap |
| [Results you must check](#results-you-must-check-when-you-use-chronicle-directly) | Nothing | Check `IsSuccess` |
| [The first append into a scope](#the-first-append-into-a-scope) | Nothing | Opt in |
| [Where the guard stops](#where-the-guard-stops) | Nothing | Outside the guarantee |
| [Tests without the in-process log](#tests-without-the-in-process-log) | Nothing | `UseDecisionReads()` |

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

A check followed by an append leaves a gap, with or without read protection. Two requests both see "not taken" and both append.

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

`IMemberDirectory` is an unknown dependency: read protection cannot see it, so nothing reports it, and it works in every test that runs one request at a time. Use a [constraint](./scenarios/unique-and-once.md). The same holds for a validator that asks the question.

## A read that is not protected

Read protection guards the reads it can see. These stay silent:

| Situation | Why it is silent |
| --- | --- |
| The command or its project is `Off` or `Measure` | A plain read is a snapshot. `Measure` only reports |
| A read marked `[Unprotected]` | It is declared advisory. Nothing guards it, and a reviewer must judge the reason |
| A service, another database or an HTTP call | Unknown. Outside the guarantee |
| A model type marked `[Unprotected]` | Every command that reads it treats it as advisory |

Under `Off`, [ARCCHR0011](../code-analysis/ARCCHR0011.md) reports plain reads in event-producing commands at Info severity, which is easy to miss in a build log. Raise it for projects that have not moved yet:

```ini
[*.cs]
dotnet_diagnostic.ARCCHR0011.severity = warning
dotnet_diagnostic.ARCCHR0012.severity = warning
```

## An immediate append under Off

Injecting `IEventLog` and calling `Append` writes at once. The append reads the tail at the moment you call it, ignores the command's unit of work and its guards, and stays in the log if the command later fails. Arc watches the appends: a failed immediate append fails the command, but a successful one cannot be undone.

Under `On` this is refused (`ImmediateAppendWhileReadsAreProtected`). Under `Off` and `Measure` it is silent. [ARCCHR0007](../code-analysis/ARCCHR0007.md) reports `IEventLog` parameters on `Handle()` and `Provide()`, and [ARCCHR0012](../code-analysis/ARCCHR0012.md) reports the direct append in a `[ProtectedDecision]` command and under `On`. Return the event from `Handle()`, or stage it with `eventLog.Transactional.Append(...)`.

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

The first command ends successfully even when the copy was already lent: the failure is in the result that nothing reads. The second returns it, and Arc turns it into the command's outcome.

`Commit()` is also the last staging step. It commits the whole command unit, so do not stage anything afterwards. Under `On`, a read or an append after `Commit()` is refused, and [ARCCHR0035](../code-analysis/ARCCHR0035.md) reports it at build time.

## No event source id, or two candidates

A command with no event source id property gets a new random id on every call, so nothing can ever conflict. A raw `Guid` is not an event source id.

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

Declare the id as `EventSourceId` (or a type derived from it) so the event goes to the account's stream. When a command has two such properties, Arc uses the first one declared, which also becomes the key of a read, and [ARCCHR0002](../code-analysis/ARCCHR0002.md) is the only signal (a warning). `[Key]` on one of them does not silence it.

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

The second command is the fix: `ICanProvideEventSourceId` names the stream explicitly. See [resolving the event source id](../resolving-event-source-id.md). A command that appends to both entities returns `EventForEventSourceId` for each; the analyzer accepts that too.

## The model folds a different key than the one the facts are written to

A guarded read folds the events of its key. Events written to another event source are invisible to it.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using FluentValidation;

[Command]
public record TakeLoanOnItsOwnStream(EventSourceId Member, string Isbn)
{
    public EventForEventSourceId Handle() => new(EventSourceId.New(), new LoanTaken(Isbn));
}

public class TakeLoanOnItsOwnStreamValidator : CommandValidator<TakeLoanOnItsOwnStream>
{
    public TakeLoanOnItsOwnStreamValidator(MemberLoans? loans) =>
        RuleFor(command => command.Isbn)
            .Must(_ => loans is not { Active: >= 3 })
            .WithMessage("The member has reached the loan limit.");
}
```

`MemberLoans` counts `LoanTaken` on the member's stream, but each loan is written to a new stream. The count is always zero, the limit never triggers, and the guard watches a stream no one writes to. Write the facts to the key you read (`return new LoanTaken(Isbn)` goes to `Member`), or model the rule on one key that carries all of them. A rule over keys that can join the set later, such as loans that each live on a book stream, is not guarded: a new key is in nobody's read set.

## `concurrency: true` is not the guard

`concurrency: true` on `[EventSourceType]`, `[EventStreamType]` and `[EventStreamId]` narrows the check Arc builds for returned events. That check reads the tail after `Handle()`, so it cannot bind your decision to what you read.

```csharp
using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[Command]
[EventSourceType("Book", concurrency: true)]
[Unprotected("deliberately advisory")]
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

Use `concurrency: true` to choose which dimensions bound the default check, not to protect a decision. [ARCCHR0016](../code-analysis/ARCCHR0016.md) reports the flag only when the same command also returns exact scopes, where it has no effect.

## An advisory reason that stopped being true

`[Unprotected("why")]` is a promise that a stale read is acceptable. The reason is only text. Put the attribute on the parameter rather than the model type unless every command that reads the type can tolerate staleness, and review the reasons when a model starts to feed a decision. [ARCCHR0032](../code-analysis/ARCCHR0032.md) reports declarations that have no effect.

## A lowered or measured mode

Two settings leave the application looking protected while it is not:

- `Measure` reports what `On` would do. It never guards.
- `ReadProtection:MaximumMode` lowers the mode at run time. Arc logs a Warning at startup for each lowered command and reports `mode_lowered` in Measure telemetry, but the commands run unprotected.

Check that no cap is set in production configuration unless you set it on purpose. See [Adopting read protection](./adopting.md#cap-the-mode-at-run-time).

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
- A scope with no sequence number and no `ExpectingNoMatchingEvent()` is skipped by the kernel, and `ConcurrencyCheckPerformed` is `false`.
- With `IDecisionReads.Get<T>` and a unit of work, commit with `IUnitOfWorkCompletionFeature.CommitAsync()` before you respond. The request middleware commits only after the endpoint has returned, and it reports no concurrency violation to the caller.
- A plain `Append` after `Get<T>` in the same request is written immediately and bypasses the read's guard. Use `Transactional.Append`.
- In an Arc project, qualify Chronicle's `ConcurrencyScopeBuilder`: `Cratis.Arc.Chronicle.Commands` has a static class with the same name.

[Chronicle used directly](./scenarios/chronicle-direct.md) shows the correct shapes.

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

The option rejects appends that succeed today, and its default is planned to change in the next major version. Read [concurrency in Chronicle](/chronicle/events/concurrency/) before you turn it on. A new aggregate and a guarded read already check their first append.

## Where the guard stops

The guard covers the events in the read set. It does not cover:

- `Revise` and `Redact` between the read and the append;
- a projection definition changed by another process, until this client registers again;
- other event sequences and external or non-Chronicle state;
- a unique-constraint index update that fails after the append is durable: Chronicle logs it, a later duplicate can pass, and a reindex repairs it;
- constraints across namespaces: tenants in different namespaces can claim the same value.

The guard also does not protect a writer that decides without one. A guard detects every matching event appended after its boundary, whoever wrote it. It only protects decisions that carry a guard, so the rule holds only when every writer that decides on it is guarded, or when it is a constraint, which the kernel checks for every append.

## Tests without the in-process log

A scenario without `UseDecisionReads()` does not exercise guarded reads. `AppendConcurrently` still queues a competing writer before the command commits, so you can test an explicit concurrency scope or a stream decision. Pinned read models cannot supply a guard. For protected reads, seed real events and use the in-process log. See [Test the race](./scenarios/testing.md).

## Where to go next

- [Choosing how a command's rule holds](./index.md): the decision matrix.
- [Refusals](./refusals.md): the failures that raise an error.
- [Scenarios](./scenarios/index.md): the correct shape for each rule.
- [Code analysis](../code-analysis/index.md): every `ARCCHR` rule.
- [Chronicle's Dynamic Consistency Boundary FAQ](/chronicle/dynamic-consistency-boundary/faq/): the same guards from Chronicle's side.
