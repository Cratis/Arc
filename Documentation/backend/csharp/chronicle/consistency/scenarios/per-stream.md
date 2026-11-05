---
title: Cratis Arc consistency for one stream
description: Enforce a monthly withdrawal limit in Cratis Arc with a per-stream model, or use a stream decision to record a report approval only once.
---

Use a per-stream read model in Cratis Arc when a rule depends on one stream's accumulated state, such as an account's withdrawals for a month. The command's route selects the month, so October's withdrawals do not make November's command conflict. Under read protection `On`, Arc guards the stream used by the validator.

Start with an [Arc application connected to Chronicle](../../add-event-sourcing.mdx). The monthly example enables `On` on the command itself. If you only need to check whether a fact is present in a short history, use the [stream decision](#a-period-approval) example instead.

## Choose the boundary

| Rule lives in… | Use |
| --- | --- |
| Folded state of one stream on the command's own route | Per-stream model |
| The entity's whole history, such as a balance | Whole-source model. Combining it with a per-stream model gives a whole-source guard |
| “Has fact X happened in this short stream?”, without a model | Stream decision |
| Rich behaviour across the whole entity | Aggregate. Do not also read a model of the same source |
| Another month or another source | `IDecisionReads` stream overload directly in `Handle()`, with a reasoned ARCCHR0040 suppression linked to the tracking feature |
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

`MonthlyWithdrawals` adds the month's `MoneyWithdrawn` amounts into `Total`. It keeps the account as its `Id`; the month selects which events to fold, not a new model key. Chronicle makes the model passive, meaning it is folded for the selected stream instead of maintained as a stored whole-account instance.

Arc resolves `{Month}` before constructing the validator. A missing or blank `Month` returns `400` naming that property, without reading. With a monthly total of `4900`, an amount of `100` passes and appends one `MoneyWithdrawn` to that month. An amount of `101` returns `Monthly limit reached` and appends nothing. A month without history reaches the nullable parameter as `null`, which this rule treats as zero.

Read protection guards the event types and route used by that fold:

```text
guard account: types=[MoneyWithdrawn], sourceType=BankAccount, streamType=Transactions, streamId=2026-10, after <position before the read>
```

If two commands read `4900` for October and each tries to withdraw `100`, a successful append by one invalidates the other's guard. The losing commit returns `400`, `concurrencyViolation`, `ReadChanged`, and writes nothing. November activity does not conflict.

If both the validator and `Handle()` take the same model, they share one enrolled fold and released instance. A cached read is still checked when it is enrolled again.

Under `Off` and `Measure`, the same parameter folds only that month but supplies no guard. Measure reports the stream dimensions and `coarsened` when another read drops them. Existing models without the stream declaration keep their existing behavior.

### Query one month

Use `ForMonth` to show the total for one account and month outside the command. This model-bound query calls the plain stream overload, taking the source and stream types from the declaration. Querying October returns that account's October total and enrolls no guard. A key-only lookup cannot identify the month; Chronicle refuses it, collection reads and `Watch` for this model.

### Keep the route exact

- The command needs a matching source type and stream type, and an explicit, non-default stream id. `[EventSource<BankAccountEventSource>("Transactions")]` keeps writers aligned. Matching string routes work too.
- Another definition, stream, `All`, or a missing stream id is refused in every mode with `ReadModelStreamNotOnCommandRoute`. [ARCCHR0037](../../code-analysis/ARCCHR0037.md) catches source-visible mismatches.
- Do not put `[LookUpBy]` on a per-stream parameter. The source always comes from the command's route. Read another account or month explicitly through `IDecisionReads` directly in `Handle()` with a reasoned ARCCHR0040 suppression linked to the tracking feature; see [stream reads](../../read-models/injecting-into-commands.md#read-a-projection-within-one-stream).
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

For owner `owner-1` and month `2026-10`, the decision reads `Approval/owner-1:2026-10` for this report. If `ReportApproved` is absent, the handler returns one approval with a guard for that stream. Any competing event there after the read rejects the append, including a race to record the first approval. Another month does not conflict.

If approval is already present, `decision.Append([])` returns no events and no guard: the retry records nothing and performs no further concurrency check. The whole stream crosses the wire for the read, so keep this history short.

Choose a stream decision or a per-stream validator, not both. The validator reads before `Provide()`; a later stream-decision boundary may not cover it and is refused with `ExplicitScopeDoesNotCoverReads`. [ARCCHR0036](../../code-analysis/ARCCHR0036.md) warns about that combination. An aggregate plus a per-stream read of the same source is refused too.

To specify the monthly limit or approval behavior, seed events on the command's route and check the resulting event or rejection. [Specify a state-dependent rule](./testing.md) explains the scenario setup without simulating concurrent writers.

## Related

- [Process streams and ordered onboarding](../../commands/event-source-definition-scenarios.md#per-stream-onboarding-state)
- [Stream decisions](../../commands/stream-decisions.md)
- [Chronicle: per-stream models](/chronicle/read-models/per-stream/)
