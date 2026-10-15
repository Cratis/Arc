---
title: One stream
description: A rule over one stream of an entity, such as one month of an account or one approval period, written as a stream read in Provide or as a stream decision.
---

Use this when the facts that decide a command live in one stream of an entity, not in its whole history: the withdrawals of one month, the approval of one period.

| Rule | Mechanism |
| --- | --- |
| At most 5000 may be withdrawn per account per month | A stream-scoped read in `Provide()` ([below](#a-monthly-limit)) |
| A report is approved once per owner per month | A stream decision ([below](#a-period-approval)) |

## Per-stream read models

A read model that belongs to one stream of an event source, so that reading it guards only that stream, is coming in this release. It will replace the stream-scoped read in [A monthly limit](#a-monthly-limit) and will be an alternative to the stream decision in [A period approval](#a-period-approval). Both of the shapes below keep working.

## A monthly limit

| | |
| --- | --- |
| Rule | At most 5000 may be withdrawn per account per month. The months are streams of one account |
| Mechanism | `IDecisionReads.Get` with the source, stream type and stream id, in `Provide()` |
| Alternatives | A stream decision (raw events, the whole stream crosses the wire). A read of the whole source: it works, but a deposit in another month conflicts |
| Not | A plain `T` parameter for a stream-keyed projection: it is refused |

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

[EventSource("BankAccount")]
[EventStream("Transactions")]
public class BankAccountEventSource : IEventSource;

public record AccountId(string Value) : EventSourceId(Value);

[EventType]
public record MoneyWithdrawn(decimal Amount);

public record MonthlyWithdrawals(
    EventSourceId Id,
    [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Total);

[Command]
[EventSource<BankAccountEventSource>("Transactions")]
[EventStreamId("{Month}")]
public record Withdraw(AccountId Account, string Month, decimal Amount)
{
    public const decimal MonthlyLimit = 5000m;

    public Task<DecisionRead<MonthlyWithdrawals>> Provide(IDecisionReads reads) =>
        reads.Get<MonthlyWithdrawals>(Account, "Transactions", Month, "BankAccount");

    public Result<MoneyWithdrawn, ValidationResult> Handle(DecisionRead<MonthlyWithdrawals> month) =>
        (month.Instance?.Total ?? 0m) + Amount > MonthlyLimit
            ? ValidationResult.Error("The monthly withdrawal limit would be exceeded.")
            : new MoneyWithdrawn(Amount);
}
```

```text
guard account: types=[MoneyWithdrawn], streamType=Transactions, streamId=2026-10, after <tail before the read>
```

A withdrawal in another month, or a deposit in this one, does not conflict.

What to know:

- The key and stream are explicit in the call. The fold and the guard use the same dimensions. An injected `DecisionRead<T>` or plain parameter is not narrowed to the command's stream.
- This rule needs a value computed from the read, so it is in `Handle()` and returns a `Result`. `Handle()` runs after `Provide()`, so the read is made before the rule decides.
- A second read of the same source with a different stream widens the guard to every month. A whole-source read of the same source drops the stream dimensions altogether. See [How it works](../how-it-works.md#several-reads).
- The stream type must be specific, not `All`. Chronicle's [stream-scoped decision read](/chronicle/read-models/decision-reads/#scoped-to-one-event-stream) lists the key requirements.

## A period approval

| | |
| --- | --- |
| Rule | A monthly report can be approved once. Approving decides on the events of its own approval stream |
| Mechanism | A stream decision: `IStreamReads` in `Provide()`, `decision.Append(...)` in `Handle()` |
| Alternatives | A per-stream read model, an aggregate for each period |
| Not | A read of the whole source beside the stream decision: the stream scope is narrower than the read, so the command is refused |

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;

[EventSource("Report")]
[EventStream("Approval", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class ReportEventSource : IEventSource;

public record ReportId(string Value) : EventSourceId(Value);

[EventType]
public record ReportApproved;

[Command]
[EventSource<ReportEventSource>("Approval")]
[EventStreamId("{Owner}:{Month}")]
public record ApproveReport(ReportId Report, string Owner, string Month)
{
    public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand();

    public EventsWithConcurrencyScopes Handle(StreamDecision decision) =>
        decision.Has<ReportApproved>()
            ? decision.Append([])
            : decision.Append(new ReportApproved());
}
```

```text
guard report: any type, streamType=Approval, streamId=owner-1:2026-10, after <tail of the stream before the read>
```

A competing event in that stream after the read rejects the command, including when both commands try the first append. Every event type in the stream counts. The whole stream crosses the wire on every command, so use this for short streams. Adding a whole-source read model to this command refuses it with `ExplicitScopeDoesNotCoverReads`, and [ARCCHR0036](../../code-analysis/ARCCHR0036.md) reports it at build time.

The empty append is an identical retry: approval was already recorded, so there is nothing to write. It succeeds without checking the stream again.

## Related

- [Stream decisions](../../commands/stream-decisions.md)
- [Route a command through an event source definition](../../commands/event-source-definitions.md)
- [Chronicle: scenarios](/chronicle/dynamic-consistency-boundary/scenarios/)
