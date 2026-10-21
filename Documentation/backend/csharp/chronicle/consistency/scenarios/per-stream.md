---
title: One stream
description: Enforce a monthly limit with a per-stream read model and a validator, or check raw facts with a stream decision.
---

Use a per-stream read model when a rule depends on the folded state of one stream on the command's own route. October's withdrawals should not make November's command conflict. Declare the model's stream, take it as a plain parameter, and put the rule in a validator. Under `On`, Arc guards that stream.

## Choose the boundary

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | Per-stream model |
| The entity's whole history, such as a balance | Whole-source model. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Rich behaviour across the whole entity | Aggregate. Do not also read a model of the same source |
| Another month or another source | `IDecisionReads` stream overload in `Provide()` |
| Uniqueness across sources | Constraint |

## A monthly limit

| | |
| --- | --- |
| Rule | At most 5000 may be withdrawn per account per month |
| Mechanism | A per-stream model and a `CommandValidator<T>` under `On` |
| Alternatives | A stream decision for short raw-event checks; a whole-source model when every month must count |
| Not | A key-only read of a per-stream model: Chronicle refuses it |

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using FluentValidation;

[EventSource("BankAccount")]
[EventStream("Transactions")]
public class BankAccountEventSource : IEventSource;

public record AccountId(string Value) : EventSourceId(Value);

[EventType]
public record MoneyWithdrawn(decimal Amount);

[ReadModel, EventStream<BankAccountEventSource>("Transactions")]
public record MonthlyWithdrawals(
    AccountId Id,
    [AddFrom<MoneyWithdrawn>(nameof(MoneyWithdrawn.Amount))] decimal Total)
{
    public static async Task<MonthlyWithdrawals?> ForMonth(AccountId account, EventStreamId month, IReadModels models)
    {
        var instance = await models.GetInstanceById<MonthlyWithdrawals>(account, month);
        return instance is null ? null : await models.Release(instance);
    }
}

[Command, ReadProtection(ReadProtectionMode.On)]
[EventSource<BankAccountEventSource>("Transactions")]
[EventStreamId("{Month}")]
public record Withdraw(AccountId Account, string Month, decimal Amount)
{
    public MoneyWithdrawn Handle() => new(Amount);
}

public class WithdrawValidator : CommandValidator<Withdraw>
{
    public WithdrawValidator(MonthlyWithdrawals? month) =>
        RuleFor(command => command.Amount)
            .Must(amount => (month?.Total ?? 0m) + amount <= 5000m)
            .WithMessage("Monthly limit reached");
}
```

The model keeps the account as its `Id`; the month selects the fold, not a new model key. Chronicle makes the model passive, so a stored instance never mixes months. Arc resolves `{Month}` before constructing the validator. A missing or blank `Month` returns `400` naming that property, without reading. The validator rejects an exceeded limit; `Handle()` only returns the event.

```text
guard account: types=[MoneyWithdrawn], sourceType=BankAccount, streamType=Transactions, streamId=2026-10, after <position before the read>
```

If another withdrawal arrives in October after the validator's read, commit returns `400`, `concurrencyViolation`, `ReadChanged`, and writes nothing. November activity does not conflict. Validator and `Handle()` parameters of the same model share one enrolled fold and released instance. A cached read is still checked when it is enrolled again.

Under `Off` and `Measure`, the same parameter folds only that month but supplies no guard. Measure reports the stream dimensions and `coarsened` when another read drops them. Existing models without the stream declaration keep their existing behavior.

### Query one month

`ForMonth` is a model-bound query on the `[ReadModel]` record above. It calls the plain stream overload, which takes the source and stream types from the declaration. It returns October for that account, not a stored whole-account total. It guards nothing. Do not use a key-only lookup, collection read or `Watch` for this model; Chronicle refuses them.

### Keep the route exact

- The command needs a matching source type and stream type, and an explicit, non-default stream id. `[EventSource<BankAccountEventSource>("Transactions")]` keeps writers aligned. Matching string routes work too.
- Another definition, stream, `All`, or a missing stream id is refused in every mode with `ReadModelStreamNotOnCommandRoute`. [ARCCHR0037](../../code-analysis/ARCCHR0037.md) catches source-visible mismatches.
- Do not put `[LookUpBy]` on a per-stream parameter. The source always comes from the command's route. Read another account or month explicitly through `IDecisionReads` in `Provide()`; see [stream reads](../../read-models/injecting-into-commands.md#read-a-projection-within-one-stream).
- A whole-source read on the same account chooses a whole-source guard. Another stream read can widen it too. Declared `Concurrency` that omits stream dimensions can also widen it; [ARCCHR0038](../../code-analysis/ARCCHR0038.md) reports source-visible cases. Use no declared policy for the model's exact event types, or stream-level `EventSourceId | EventStreamType | EventStreamId` for an any-event stream boundary.
- The declaration's source type filters both fold and guard. Events written without that source type are not included. Every writer must use the same definition route. Revisions and redactions remain outside the guard.

## A period approval

Use a stream decision for the raw question “has this report been approved in this short stream?”. Use a per-stream model when you need an approval count or folded status; the [period approval model](../../commands/event-source-definition-scenarios.md#per-stream-approval-state) shows that alternative.

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

The decision reads and guards `Approval/owner-1:2026-10` for this report. Any competing event in that stream after the read refuses the append, including the first append. Another month does not conflict. The whole stream crosses the wire, so keep it short. The empty append is an identical retry: approval was already recorded, so nothing is written or checked again.

Choose a stream decision or a per-stream validator, not both. The validator reads before `Provide()`; a later stream-decision boundary may not cover it and is refused with `ExplicitScopeDoesNotCoverReads`. [ARCCHR0036](../../code-analysis/ARCCHR0036.md) warns about that combination. An aggregate plus a per-stream read of the same source is refused too.

## Related

- [Process streams and ordered onboarding](../../commands/event-source-definition-scenarios.md#per-stream-onboarding-state)
- [Stream decisions](../../commands/stream-decisions.md)
- [Chronicle: per-stream models](/chronicle/read-models/per-stream/)
