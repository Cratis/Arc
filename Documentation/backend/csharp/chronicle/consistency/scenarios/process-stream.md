---
title: Cratis Arc ordered steps in a process stream
description: Use a Cratis Arc stream decision to enforce onboarding order, then complete the process stream so later commands cannot append to it.
---

Use a process stream in Cratis Arc when each onboarding or order fulfillment needs an ordered history and an explicit end. A stream decision reads the process's events and guards the next append against competing changes. Completing the stream prevents later appends to that process.

Start with an [Arc application connected to Chronicle](../../add-event-sourcing.mdx). Give each process its own `ProcessId` within the customer's `Onboarding` stream type. The example orders steps as `Identity`, `Address`, then `Consent`. `FinishOnboarding` only records the finish and requests completion; it does not check that those steps have happened.

| | |
| --- | --- |
| Rule | Onboarding steps happen in order. A finished onboarding accepts nothing more |
| Mechanism | A definition with an `Onboarding` stream per process. A stream decision for the order. `CompleteStream` for the end |
| Alternatives | An aggregate for each process |
| Not | A plain read of a status field. A declared `Concurrency` as the only rule: it is the default append check, not a decision guard |

```csharp
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Chronicle.Streams;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSources;
using Cratis.Monads;

[EventSource("Customer", Concurrency = ConcurrencyDimensions.EventSourceId)]
[EventStream("Onboarding", Concurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId)]
public class CustomerEventSource : IEventSource;

public record CustomerId(string Value) : EventSourceId(Value);

[EventType]
public record OnboardingStepCompleted(string Step);

[EventType]
public record OnboardingFinished;

[Command]
[EventSource<CustomerEventSource>("Onboarding")]
[EventStreamId("{ProcessId}")]
public record CompleteOnboardingStep(CustomerId Customer, string ProcessId, string Step)
{
    static readonly string[] _steps = ["Identity", "Address", "Consent"];

    public Task<StreamDecision> Provide(IStreamReads reads) => reads.ForCommand();

    public Result<EventsWithConcurrencyScopes, ValidationResult> Handle(StreamDecision decision)
    {
        var done = decision.EventsOf<OnboardingStepCompleted>().Count();
        return done < _steps.Length && _steps[done] == Step
            ? decision.Append(new OnboardingStepCompleted(Step))
            : ValidationResult.Error($"The next step is '{(done < _steps.Length ? _steps[done] : "none")}'.");
    }
}

[Command]
[EventSource<CustomerEventSource>("Onboarding")]
[EventStreamId("{ProcessId}")]
public record FinishOnboarding(CustomerId Customer, string ProcessId)
{
    public (OnboardingFinished, CompleteStream) Handle() => (new(), new());
}
```

`Provide()` reads the command's resolved route before `Handle()` counts the completed steps. The handler accepts only the next name in `_steps`. Its returned decision carries a guard over every event type in that process stream:

```text
guard customer: any type, streamType=Onboarding, streamId=<process>, after <tail of the stream before the read>
```

## What holds

Follow one process through these outcomes:

- On an empty stream, `Identity` appends one `OnboardingStepCompleted`. `Address` and `Consent` can then follow in order.
- On an empty stream, `Address` returns `The next step is 'Identity'.` After all three steps, another step returns `The next step is 'none'.` Neither rejection appends an event.
- If two valid step commands decide from the same stream tail, the first successful append makes the other's guard fail. Nothing is written for the loser.
- Once `FinishOnboarding` has committed its event and completed the stream, a later append from any command fails with a validation result naming the closed stream. Another customer's process with the same id stays open.

The guard covers the whole stream because every event in this short process history matters to the next decision.

## What does not hold

Appending `OnboardingFinished` and completing the stream are separate operations. If execution stops between them, the final event remains but the stream can stay open. Retry only the completion; see [Completing streams](../../commands/completing-streams.md) for the completion behavior.

Keep one stream for one process. Every step reads the process's history, so a stream that grows without end makes each decision read more. Declared `Concurrency` sets the default append check; it does not enforce step order by itself. Here, the handler checks the order and the stream decision protects the append.

To specify which step is accepted or rejected, use [state-dependent command specs](./testing.md) with events seeded on the process route.

## Related

- [Completing streams](../../commands/completing-streams.md)
- [Stream decisions](../../commands/stream-decisions.md)
- [A stream per process](../../commands/event-source-definition-scenarios.md#a-stream-per-process)
