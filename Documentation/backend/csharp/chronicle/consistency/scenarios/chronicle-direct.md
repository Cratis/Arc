---
title: Chronicle used directly
description: Guards written with IEventStore, IEventLog and IDecisionReads in endpoints, controllers and services of an Arc application, where nothing is enrolled for you.
---

Use this page when you write an endpoint, a controller action or a service that talks to Chronicle yourself. Chronicle gives you the same guards as inside a command. Arc's command pipeline does not run for you, so read protection is not on: you enrol the reads, check the results and commit before you respond.

| You want | Shape |
| --- | --- |
| A guard on one append | [A decision read on a detached append](#a-decision-read-on-a-detached-append) |
| The read and the append in different places | [A unit of work with decision reads](#a-unit-of-work-with-decision-reads) |
| The same in a controller | [A controller action](#a-controller-action) |
| A guard from a position you captured | [An explicit scope captured before the facts](#an-explicit-scope-captured-before-the-facts) |
| A value claimed once | [A value claimed through the event source id](#a-value-claimed-through-the-event-source-id) |

To have Chronicle refuse unguarded reads and immediate appends in your own host code, see [Read protection in Chronicle](/chronicle/read-models/read-protection/).

All examples on this page share one namespace.

```csharp
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

[EventType]
public record CopyAdded();

[EventType]
public record BookIssued(Guid MemberId);

[EventType]
public record LoanTaken(string Isbn);

public record BookCopies(
    EventSourceId Id,
    [Increment<CopyAdded>, Decrement<BookIssued>] int Available);

public record MemberLoans(
    EventSourceId Id,
    [Increment<LoanTaken>] int Active);
```

## A decision read on a detached append

Read a `GetDetached` decision read from the event store and pass it to `AppendMany` in `guardedBy`.

```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Microsoft.AspNetCore.Http;

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

        if (result.GetDecisionConflicts([stock]).Any())
        {
            return Results.Conflict("The book's copies changed. Nothing was written; read again and decide again.");
        }

        return result.IsSuccess ? Results.Ok() : Results.UnprocessableEntity("The loan was not recorded.");
    }
}
```

The boundary is captured before the fold, and the append is rejected if a `CopyAdded` or `BookIssued` arrived for the book since. `AppendMany` returns a result: a violation is a value, not an exception, so always check it. Check `GetDecisionConflicts()` before you build the response, and return your own message: a `DecisionConflict` carries the read model's `Type` and cannot be serialized as a response body. The batch must go to the event log of the same store and namespace, and needs at least one read.

## A unit of work with decision reads

Use `IDecisionReads.Get<T>` when the read and the append are in different places. The request's unit of work enrols the read, you stage the event with `Transactional.Append`, and you commit before you respond.

```csharp
using Cratis.Chronicle.AspNetCore.Transactions;
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
            if (unit.GetDecisionConflicts().Any())
            {
                return Results.Conflict("The member's loans changed. Nothing was written; read again and decide again.");
            }

            return unit.IsSuccess ? Results.Ok() : Results.UnprocessableEntity("The loan was not recorded.");
        });
}
```

Two parts matter:

- `Get<T>` needs an ambient unit of work. In an Arc app the Chronicle request middleware begins one per request.
- The middleware commits *after* the endpoint returns, and reports no concurrency violation. Commit with `IUnitOfWorkCompletionFeature.CommitAsync()` before responding and use the result, or the caller sees success for a write that was rejected.

Do not mix the two styles: a plain `eventLog.Append(...)` in the same request is written immediately and bypasses the guard.

## A controller action

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
        if (unit.GetDecisionConflicts().Any())
        {
            return Conflict("The member's loans changed. Nothing was written; read again and decide again.");
        }

        return unit.IsSuccess ? Ok() : UnprocessableEntity("The loan was not recorded.");
    }
}
```

## An explicit scope captured before the facts

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

## A value claimed through the event source id

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

## Related

- [Pitfalls: results you must check](../pitfalls.md#results-you-must-check-when-you-use-chronicle-directly)
- [Chronicle: read protection](/chronicle/read-models/read-protection/)
- [Chronicle: decision reads](/chronicle/read-models/decision-reads/)
