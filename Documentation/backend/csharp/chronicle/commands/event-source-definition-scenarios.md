---
title: Event source definition scenarios
description: Route commands through an event source definition for a stream per entity, per owner and period, and per process, then decide against a stream and complete one.
---

Use this page when you know the shape of the stream you need and want the command. Each scenario states the shape, names one recommended way with the reason, shows a complete definition and command, says what the append is checked against, and lists the alternatives. They follow [Route a command through an event source definition](./event-source-definitions.md); read that page first for the declarations.

| Shape | Event source id | Stream | Stream id |
| --- | --- | --- | --- |
| [Per entity](#a-stream-per-entity) | The entity | One stream | Default |
| [Per owner and period](#a-stream-per-owner-and-period) | The report | One stream | `{Owner}:{Period}` |
| [Per process](#a-stream-per-process) | The customer | One stream | `{ProcessId}` |

One boundary note applies to all three: a definition scope and a stream decision guard the whole source or stream, whatever the event type. In Dynamic Consistency Boundary terms that is a coarse, aggregate-like boundary. It is right when every event in the stream matters to the decisions made against it, which is why these examples use short, cohesive streams.

Chronicle has one event source per event. Every shape here chooses a stream within one event source; a rule over several entities is several events, one per event source, in one batch. See [Chronicle's DCB scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) for that side.

## A stream per entity

**Shape:** everything that happens to one account is one history, and a competing write to the same account conflicts.

**Recommended:** a definition with the `EventSourceId` dimension and one named stream, and `[EventSource<TSource>(stream)]` on the command. The reason: the stream and its guard are declared once and typo-checked. If one unnamed history per account is enough, you need no definition: the command key and the default append already give that boundary.

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

**Alternatives:** `[EventStreamType("Transactions")]` and `[EventSourceType("Account")]` strings (ARCCHR0013 suggests the definition); an aggregate root when the account has rich behaviour; an exact scope when the caller supplies the revision it saw.

### Per-stream monthly state

For a monthly withdrawal limit, add `[EventStream<AccountEventSource>("Transactions")]` to a source-keyed withdrawals model and route the command with `[EventStreamId("{Month}")]`. Take the model in a validator under `On`; no decision token belongs on the command. The [complete monthly-limit example](../consistency/scenarios/per-stream.md#a-monthly-limit) uses a definition without source-wide `Concurrency`, so October is guarded and November does not conflict. The source-wide policy in the per-entity example above would widen that guard to the account's whole history.

## A stream per owner and period

**Shape:** a report is approved once per owner and month. Another owner's month, and another month of the same owner, are independent.

**Recommended:** a template in `[EventStreamId]` on a definition-routed command. The reason: it is one attribute, the id is built the same way for the append, a read and a completion, and `EventStreamIdTemplate.ResolveFor(command)` reuses it in tests.

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

**Alternatives:** `ICanProvideEventStreamId` when code computes the id and a template cannot express it; `[EventStreamType]` and `[EventStreamId]` strings without a definition.

### Decide against the stream

**Shape:** the same report, but approving twice must not record a second approval, and two simultaneous approvals must not both succeed.

**Recommended:** a [stream decision](./stream-decisions.md), because the rule is "has this fact already happened in this stream" and the facts are raw events in one short stream. Alternatives: a read model parameter under [read protection](../consistency/getting-started.md) `On` when the rule needs folded state or several keys, and a constraint when the rule is only uniqueness.

Add the stream decision. `ForCommand()` reads the stream the command declares, definition and template included, and `Append(...)` returns the events with an exact scope at the tail it read.

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

The scope covers every event in the stream, not only `ReportApproved`, and the whole stream crosses the wire on each command. Keep these streams short and cohesive: a period is a good stream, the life of the company is not. See [stream decisions](./stream-decisions.md) for the exact-scope rules and [how a stream decision differs from a decision read](../consistency/faq.md#event-source-definitions-and-stream-decisions).

Application specs give state and assert outcomes, not concurrency simulations. Resolve the definition's route when giving prior facts; Arc's framework specs prove the scope's conflict handling. This excerpt assumes the definition and command above:

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

The same shape can enforce a required number of approvals using `Approvals`. Keep the [stream decision](#decide-against-the-stream) for the raw “approved once” check when no folded model is needed. Do not combine its later boundary with a validator-first read.

## A stream per process

**Shape:** each customer onboarding is its own process with its own id. When the process ends, nothing more may be appended to it, and another customer's process with the same id stays open.

**Recommended:** a stream per process id on a definition-routed command, and `CompleteStream` returned by the command that ends the process. The reason: the command already declares the route, so the completion needs no repeated type or id. Alternatives: `route.Complete()` from a reactor or another command, and an exact `CompleteStream(streamType, streamId)` when the target differs from the command's route.

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

`new CompleteStream()` takes the stream type and stream id from the command's resolved route, so it closes `Onboarding` / `ProcessId`. Because the command has an event source id, Arc scopes the completion to that customer: another customer's process with the same id stays open. Details and limits:

- Arc completes the stream only after the command's events commit. The append and the completion are separate operations with no expected-tail guard between them.
- A later append to the completed stream, from any command, fails with a validation result naming the closed stream.
- The default stream id cannot be completed. A command that declares no stream id gets a validation failure.
- Completing an already completed stream succeeds, so a repeated completion is safe.
- Completion needs Chronicle kernel 19.39 or later.

A reactor or another command can close the same stream from a route instead of repeating the declaration: `routes.For<CustomerEventSource>("Onboarding").WithStreamId(processId).Complete()` returns the `CompleteStream` for it. See [completing streams](./completing-streams.md) for reactors and for asserting completion in a scenario.

## Where to go next

- [How routing works](./event-source-definition-routing.md): what Arc does from the attribute to the append.
- [Choosing how a command's rule holds](../consistency/index.md) and its [scenarios](../consistency/scenarios/per-stream.md#a-period-approval): when a stream decision is the right guard.
- [Chronicle's Dynamic Consistency Boundary scenarios](/chronicle/dynamic-consistency-boundary/scenarios/) and [event sources](/chronicle/event-sources/): stream metadata plays the role of DCB tags, and a definition is the typed way to choose which dimensions guard an append.
