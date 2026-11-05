---
title: Cratis Arc event source scenarios
description: Choose a Cratis Arc event stream per entity, owner and period, or process. Use the examples to route commands, prevent duplicate approvals and close streams.
---

Cratis Arc event source definitions can route commands to one history per entity, owner and period, or process. Choose the history your rule needs below, then add a read guard if the command decides from existing state. The examples build on [event source definitions](./event-source-definitions.md) and show where routing alone is not enough.

| Shape | Event source id | Stream | Stream id |
| --- | --- | --- | --- |
| [Per entity](#a-stream-per-entity) | The entity | One stream | Default |
| [Per owner and period](#a-stream-per-owner-and-period) | The report | One stream | `{Owner}:{Period}` |
| [Per process](#a-stream-per-process) | The customer | One stream | `{ProcessId}` |

These examples use short streams where every event matters to the decision. Both definition scopes and stream decisions guard the selected history across all event types; neither isolates just one fact such as an approval.

Each event belongs to one event source. If a rule spans several entities, append an event for each source in one batch and guard the relevant histories. [Chronicle's Dynamic Consistency Boundary scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) cover those cross-entity decisions.

## A stream per entity

Use one account history when writes for that account should compete, regardless of stream or event type. Select the `EventSourceId` dimension on the definition and route the command through its named stream. This declares the stream and append policy once, with checks for undeclared stream names.

If one unnamed history per account is enough, use the command key and default stream without a definition.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

[EventSource("Account", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Transactions")]
public class AccountEventSource : IEventSource;

[EventType]
public record TransactionRecorded(decimal Amount);

[Command]
[EventSource<AccountEventSource>("Transactions")]
public record RecordTransaction(EventSourceId AccountId, decimal Amount)
{
    public TransactionRecorded Handle() => new(Amount);
}
```

The event lands on event source `AccountId`, event source type `Account`, stream type `Transactions`, default stream id. The dimension is on the event source, so the `Transactions` stream inherits it. The append is checked against all events for the same event source id, of any type.

This is the default append check: it does not bind `Handle()`'s decision to what it read. For a balance rule, use a read model parameter under [read protection](../consistency/getting-started.md) `On`, or an [aggregate](../aggregates/injecting-into-commands.md).

Existing `[EventStreamType("Transactions")]` and `[EventSourceType("Account")]` strings still route events, though ARCCHR0013 suggests the matching definition. Use an aggregate for account behaviour that needs its state, or an exact scope when the caller supplies the revision it saw.

### Per-stream monthly state

For a monthly withdrawal limit, add `[EventStream<AccountEventSource>("Transactions")]` to a source-keyed withdrawals model and route the command with `[EventStreamId("{Month}")]`. Take the model in a validator under `On`; no decision token belongs on the command. The [complete monthly-limit example](../consistency/scenarios/per-stream.md#a-monthly-limit) uses a definition without source-wide `Concurrency`, so October is guarded and November does not conflict. The source-wide policy in the per-entity example above would widen that guard to the account's whole history.

## A stream per owner and period

Give each owner and month a separate approval history within a report. A template in `[EventStreamId]` makes that identity consistent across appends, reads and completions. `EventStreamIdTemplate.ResolveFor(command)` also lets specs resolve the same id without duplicating its format.

This first command records approvals; it does not yet prevent a second one. Add the stream decision below when approval must happen only once.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

[EventSource("Report")]
[EventStream("Approval", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class ReportEventSource : IEventSource;

[EventType]
public record ReportApproved;

[Command]
[EventSource<ReportEventSource>("Approval")]
[EventStreamId("{Owner}:{Period}")]
public record ApproveReport(EventSourceId ReportId, string Owner, DateOnly Period)
{
    public ReportApproved Handle() => new();
}
```

For `Owner = "acme"` and `Period = 2026-10-01`, the append targets event source `ReportId`, stream type `Approval`, stream id `acme:2026-10-01`. The `Approval` stream declares its own dimensions, so the check is narrowed to events with that stream type and stream id for the report. A write to `acme:2026-11-01` is not part of it.

The template reads public properties of the command. Arc normally resolves it after validation. A per-stream validator parameter needs the route earlier, so Arc resolves it before constructing that validator; a blank part returns `400` naming the property without reading. Keep the id deterministic: two commands that compute different ids for the same period do not conflict.

Use `ICanProvideEventStreamId` instead when the id needs logic a template cannot express. `[EventStreamType]` and `[EventStreamId]` also work without a definition if you do not need shared stream declarations.

### Decide against the stream

To prevent duplicate approvals, read the approval history with a [stream decision](./stream-decisions.md). It captures the tail before reading, so two commands cannot both append a first approval based on the same empty history.

`ForCommand()` reads the route declared by the command, including the definition and template. `Append(...)` returns events with a scope tied to that captured position. If the rule instead needs folded state or several keys, use a read model parameter under [read protection](../consistency/getting-started.md) `On`. A constraint is another option for a uniqueness rule.

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

[EventType]
public record ReportApproved;

[Command]
[EventSource<ReportEventSource>("Approval")]
[EventStreamId("{Owner}:{Period}")]
public record ApproveReportOnce(EventSourceId ReportId, string Owner, DateOnly Period)
{
    public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand();

    public EventsWithConcurrencyScopes Handle(StreamDecision decision) =>
        decision.Has<ReportApproved>()
            ? decision.Append([])
            : decision.Append(new ReportApproved());
}
```

What this guards:

- The tail of `Approval` / `acme:2026-10-01` for this report is captured before the events are read.
- A competing append to that stream after the read rejects the command, including when both commands attempt the first append.
- A second approval finds `ReportApproved` and returns an empty append: an identical retry, which succeeds without a check.
- A competitor on another stream id, such as another month, does not conflict. Use `reads.StreamType(...)` to guard every stream id of the type.

The scope covers every event in the stream, not only `ReportApproved`, and the whole stream is transferred on each command. Keep it bounded to the history the decision needs. See [stream decisions](./stream-decisions.md) for exact-scope rules and [the comparison with decision reads](../consistency/faq.md#event-source-definitions-and-stream-decisions).

In an application spec, seed a prior approval on the same route, execute the command and assert that it appends no new approval. Arc's framework specs cover conflict handling. The setup below uses the definition and command above:

```csharp
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

await using var scenario = new CommandScenario<ApproveReportOnce>();
var id = EventSourceId.New();
var route = scenario.EventRoutes.For<ReportEventSource>("Approval").WithStreamId("acme:2026-10-01");
scenario.Given.ForEventSource(id).OnRoute(route).Events(new ReportApproved());
```

### Per-stream approval state

When the rule needs a count or a folded status, use a per-stream model instead of reading raw events. This excerpt reuses `ReportEventSource` and `ReportApproved` above. Under `On`, the validator prevents a second approval and a concurrent approval in the same owner/period refuses the commit with `ReadChanged`; another period does not conflict.

```csharp
using Cratis.Arc.Commands;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using FluentValidation;

public record ReportId(string Value) : EventSourceId(Value);

[ReadModel, EventStream<ReportEventSource>("Approval")]
public record PeriodApproval(
    ReportId Id,
    [Increment<ReportApproved>] int Approvals,
    [SetValue<ReportApproved>(true)] bool Approved);

[Command, ReadProtection(ReadProtectionMode.On)]
[EventSource<ReportEventSource>("Approval")]
[EventStreamId("{Owner}:{Period}")]
public record ApprovePeriod(ReportId Report, string Owner, DateOnly Period)
{
    public ReportApproved Handle() => new();
}

public class ApprovePeriodValidator : CommandValidator<ApprovePeriod>
{
    public ApprovePeriodValidator(PeriodApproval? approval) =>
        RuleFor(command => command.Report)
            .Must(_ => approval?.Approved != true)
            .WithMessage("This period is already approved.");
}
```

Use `Approvals` when the rule needs a count rather than just the approved status. For a check that needs only the raw approval fact, keep the [stream decision](#decide-against-the-stream). Choose one read path for that rule rather than mixing a validator-first model read with a later stream decision.

## A stream per process

Give each customer onboarding process its own stream id. When onboarding finishes, return `CompleteStream` with the final event so later commands cannot add steps to the completed stream. The command's customer id scopes completion to that customer; another customer's process with the same id stays open.

The completion uses the route already declared on the command. If another command or reactor closes the process, use `route.Complete()`. Supply `CompleteStream(streamType, streamId)` explicitly when the target differs from the command route.

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;

[EventSource("Customer", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Onboarding", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class CustomerEventSource : IEventSource;

[EventType]
public record OnboardingStepCompleted(string Step);

[EventType]
public record OnboardingFinished;

[Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)]
[EventSource<CustomerEventSource>("Onboarding")]
[EventStreamId("{ProcessId}")]
public record CompleteOnboardingStep(EventSourceId CustomerId, string ProcessId, string Step)
{
    public OnboardingStepCompleted Handle() => new(Step);
}

[Command, Cratis.Arc.Commands.ReadProtection(Cratis.Arc.Commands.ReadProtectionMode.On)]
[EventSource<CustomerEventSource>("Onboarding")]
[EventStreamId("{ProcessId}")]
public record FinishOnboarding(EventSourceId CustomerId, string ProcessId)
{
    public (OnboardingFinished, CompleteStream) Handle() => (new(), new());
}
```

`CompleteOnboardingStep` appends to event source `CustomerId`, stream type `Onboarding`, stream id `ProcessId`. `FinishOnboarding` appends the final event and then completes that stream.

### Per-stream onboarding state

This excerpt reuses the onboarding definitions and commands above. `OnboardingProgress` counts completed steps in this customer's process. The validators enforce the order `identity → address → consent` and require all three before `FinishOnboarding` returns its final event and `CompleteStream`.

```csharp
[ReadModel, EventStream<CustomerEventSource>("Onboarding")]
public record OnboardingProgress(
    EventSourceId Id,
    [Increment<OnboardingStepCompleted>] int Steps);

public class CompleteOnboardingStepValidator : CommandValidator<CompleteOnboardingStep>
{
    public CompleteOnboardingStepValidator(OnboardingProgress? progress)
    {
        string[] steps = ["identity", "address", "consent"];
        var completed = progress?.Steps ?? 0;
        RuleFor(command => command.Step)
            .Must(step => completed < steps.Length && step == steps[completed])
            .WithMessage("Complete the next onboarding step in order.");
    }
}

public class FinishOnboardingValidator : CommandValidator<FinishOnboarding>
{
    public FinishOnboardingValidator(OnboardingProgress? progress) =>
        RuleFor(command => command.ProcessId)
            .Must(_ => progress?.Steps == 3)
            .WithMessage("Complete every onboarding step first.");
}
```

The per-stream model is read before either validator runs and is guarded under `On`. Concurrent activity in this process refuses the commit; another process id does not. Completion still happens only after commit, not as part of the atomic append.

### Complete the stream

`new CompleteStream()` closes `Onboarding` / `ProcessId` for the command's customer. The events commit first and completion runs afterward, with no expected-tail guard between the two operations. If completion fails, the final event remains committed and the stream may still be open. Retry the completion rather than blindly repeating a command that emits events.

Once closed, an append from any command receives a validation result naming the stream. Repeating the completion itself succeeds. Completion requires Chronicle kernel 19.39 or later, and the stream must have a non-default id; a command without one gets a validation failure.

A reactor or another command can close the same stream from a route instead of repeating the declaration: `routes.For<CustomerEventSource>("Onboarding").WithStreamId(processId).Complete()` returns the `CompleteStream` for it. See [completing streams](./completing-streams.md) for reactors and for asserting completion in a scenario.

## Where to go next

- [How routing works](./event-source-definition-routing.md): what Arc does from the attribute to the append.
- [Choosing how a command's rule holds](../consistency/index.md) and its [scenarios](../consistency/scenarios/per-stream.md#a-period-approval): when a stream decision is the right guard.
- [Chronicle's Dynamic Consistency Boundary scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) and [event sources](/chronicle/event-sources/): stream metadata plays the role of DCB tags, and a definition is the typed way to choose which dimensions guard an append.
