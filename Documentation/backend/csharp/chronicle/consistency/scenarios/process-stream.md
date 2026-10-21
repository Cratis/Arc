---
title: Process stream
description: A process with ordered steps and an end, written as a stream per process, a stream decision for the order, and a completed stream for the end.
---

Use this when each process (an onboarding, an order fulfilment) has its own history, the steps must happen in order, and a finished process must accept nothing more.

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

```text
guard customer: any type, streamType=Onboarding, streamId=<process>, after <tail of the stream before the read>
```

## What holds

- Two steps submitted at the same time: the second one conflicts, because the first appended to the stream after the second read it. Nothing is written for the loser.
- After `FinishOnboarding`, Arc completes the stream once the command's events commit. A later append to that stream, from any command, fails with a validation result that names the closed stream. Another customer's process with the same id stays open.
- The guard is the whole stream, on purpose. A process stream is short and every event in it matters to the next decision.

## What does not hold

- Appending and completing are separate operations. If the process stops in between, the final event stays and the stream can stay open. Retrying only the completion is safe.
- A stream that grows without end makes every step read more. Keep one stream for one process.
- Declaring `Concurrency` on a stream does not make a rule hold. It makes the default append check wider. It is the right setting here because the stream decision, not the declaration, carries the rule. Declare it only for ledgers, numbering and process streams.

## Related

- [Completing streams](../../commands/completing-streams.md)
- [Stream decisions](../../commands/stream-decisions.md)
- [A stream per process](../../commands/event-source-definition-scenarios.md#a-stream-per-process)
