---
title: Migrate inline command work to Cratis Arc operations
description: Move a service write out of a Cratis Arc command handler while preserving its input and client response, then check execution and compensation.
---

Move an inline service write to a Cratis Arc command operation when `Handle()` can describe the work without needing its result. This migration keeps the booking command's input, endpoint, and caller response while making its decision testable without a reservation provider. Direct service-backed handlers remain supported when that separation does not fit.

Start with the `ReservationId`, `SeatId`, `ISeatReservations`, and `ReserveSeat` declarations from [implementing an operation](./implementing.md). The two command definitions below are alternatives. Replace the original with the new definition; do not compile both.

## Identify what the decision needs

Before moving the write, trace the values `Handle()` needs to choose the work and construct its response. Those values can come from the command, read models, or data acquired through `Provide()`. The returned operation can then carry the inputs needed to perform the write.

In this example, the caller already supplies `ReservationId`, so the response does not depend on the reservation service returning anything. If your provider creates the identifier or returns information the decision needs, this replacement will not preserve that behavior. Operations cannot feed a receipt back into a response that `Handle()` has already constructed.

Keep the direct call when you need that result, or deliberately redesign the provider contract to accept an identifier allocated before the decision. Moving the write into `Provide()` would only hide the side effect. Do not change identity or persistence semantics just to make `Handle()` pure.

## Replace the forward call with a declaration

The original handler awaits the provider call before returning the supplied reservation identifier:

```csharp
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands.ModelBound;

namespace SeatBooking;

[Command]
public record BookSeat(ReservationId ReservationId, SeatId SeatId)
{
    public async Task<ReservationId> Handle(
        ISeatReservations reservations,
        CancellationToken cancellationToken)
    {
        await reservations.Reserve(ReservationId, SeatId, cancellationToken);
        return ReservationId;
    }
}
```

Replace the service call with a `ReserveSeat` declaration, keeping the same response identifier:

```csharp
using Cratis.Arc.Commands.ModelBound;

namespace SeatBooking;

[Command]
public record BookSeat(ReservationId ReservationId, SeatId SeatId)
{
    public (ReservationId Response, ReserveSeat Operation) Handle() =>
        (ReservationId, new ReserveSeat(ReservationId, SeatId));
}
```

The command now describes the reservation. Arc executes `ReserveSeat` through the model-bound pipeline using the existing `ISeatReservations` registration; no separate operation-handler registration is needed. Because the identifier was known before execution, the response can stay the same even though `Handle()` no longer calls the provider.

The command name and input properties have not changed. After rebuilding the backend, the generated proxy still exposes a `ReservationId` response, excluding the server-consumed operation. Direct C# callers of `Handle()` now receive a tuple containing the declaration instead of awaiting the reservation. Keep execution callers on `ICommandPipeline`, which returns the outcome after operations and execution scopes complete.

:::caution[Remove the old execution path]
Do not call `Reserve()` in `Handle()` and also return `ReserveSeat`. Do not manually call the operation's `Execute()` before returning it. Either would perform the work twice. Moving the same write into `Provide()` also leaves it outside operation recovery.
:::

## Preserve the right extension boundary

If a custom `ICommandResponseValueHandler` exists only to perform this application side effect, the operation can replace that handler and its typed proxy marker. Remove the unused handler declaration rather than retaining redundant discovery and dependency requirements. Operation values are reserved for Arc's operation phase, not dispatched to ordinary response handlers again.

Specialized response value handlers still belong at the framework integration boundary, where they interpret returned values. With Cratis Chronicle, continue returning events for durable domain facts rather than wrapping appends in operations to bypass the append contract. Work that must follow committed facts still needs the delivery and recovery behavior of a reactor, outbox, or durable workflow.

A controller action does not become an operation-aware command by returning an operation object. Use the [model-bound pipeline](../command-pipeline.md), or keep the controller's existing service interaction when its HTTP behavior is the requirement.

## Check scopes and compensation before enabling the new path

Check custom execution scopes before switching callers to the replacement. These scopes surround the command with integration work such as transaction completion. Operations require explicitly compatible scopes and at most one deferred commit participant, as described in the [supported scope profile](./reference.md#supported-scope-profile). A scope that commits business changes must report that responsibility and its actual commit outcome; marking it as a nonparticipant does not make it safe.

Keep execution flat as well. A call to another command from `Execute()` or `Compensate()` is rejected. If the old handler coordinates nested commands, that composition needs review before this migration.

Review `ReserveSeat.Compensate()` as a business cancellation. It must handle an attempted reservation whose acknowledgment was lost, without canceling another owner's reservation or a previously finalized success. Check how the provider handles conflicting reuse of a request key. If no meaningful reversal exists, omit compensation and accept that failed execution may leave uncompensated work.

With Chronicle, a known commit rejection can permit compensation, but a committed, unknown, or mixed outcome suppresses it. Explicit aggregate commits can therefore change whether recovery is allowed. Arc selects recovery and attempts eligible callbacks in reverse order; a failed `CommandResult` is not proof that all work was reversed. Review the [commit and recovery contract](./reference.md#commit-and-recovery) rather than adding a rollback stack to the command.

## Prove that behavior was preserved

Compare the replacement with the old handler using [operation specifications](../../testing/command-operations.md):

1. Call `Handle()` directly and assert the response identifier and operation data without registering a provider.
2. Execute through `CommandScenario` with a controlled provider. Check the actual provider arguments and the caller response, including that failed commands expose no response.
3. Inject a forward failure. Assert that Arc preserves the original failure, stops later operations, and attempts eligible compensation, including for the operation that threw.
4. Keep validation and authorization specs, since returning an operation does not replace those checks.
5. With Chronicle, [exercise a real commit rejection](../../testing/command-operations-with-chronicle.md) and verify that rejected events do not persist.

Review retry behavior in the injected provider as well. Arc does not automatically retry operations, but an HTTP resilience policy, database execution strategy, or driver may retry inside a provider call. Those policies need idempotency: repeating a request must not repeat its effect. They must also preserve ownership so a later failed attempt cannot cancel earlier finalized work. Compensation remains in-process and uses a cooperative timeout; it does not provide recovery after a crash.

The migration is complete when the response and provider effects are preserved, the decision is independently testable, and the recovery boundary is explicit. For unexpected outcomes, use the [troubleshooting reference](./reference.md#troubleshooting).
