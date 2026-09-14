---
title: Move inline work to a command operation
description: Migrate a supported service-backed command to a pure decision and returned operation without changing its input or client response contract.
---

**Goal:** move an inline service write out of `Handle()` while keeping the same command input, endpoint, and caller response. Direct service-backed handlers remain supported; migrate when the work can be described before it executes and the separation makes the decision easier to test.

This recipe reuses the `ReservationId`, `SeatId`, `ISeatReservations`, and `ReserveSeat` declarations from [implementing an operation](./implementing.md). The two command definitions below are alternatives: compile the replacement instead of the original, not alongside it.

## Identify what the decision needs

Separate three concerns before moving code:

- **Inputs to the decision:** values on the command, read models, or data acquired through `Provide()`.
- **The decision:** which operations and domain facts should result from those inputs.
- **Execution:** the service writes performed by the returned operations.

If the provider creates an identifier or returns information that the decision or response needs, do not hide that write inside `Provide()` to make `Handle()` appear pure. Operation execution does not return a receipt into an already-constructed response. Keep the direct service call when it is the right fit, or deliberately redesign the provider contract to accept an identifier allocated before the decision. Do not change identity or persistence semantics solely for syntactic purity.

## Replace the forward call with a declaration

The supported original command performs the reservation itself:

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

Replace that command definition with:

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

The response identifier is already an input, so returning it requires no provider result. The command now describes the reservation, and Arc executes the existing `ReserveSeat` operation through the same model-bound pipeline. Keep the existing `ISeatReservations` registration; add no separate operation-handler registration.

The command name and input properties have not changed. After rebuilding the backend, the generated proxy still exposes a `ReservationId` response—not the tuple or operation. Direct C# callers of `Handle()` now receive a declaration; callers using `ICommandPipeline` continue to receive the actual execution result.

:::caution[Remove the old execution path]
Do not call `Reserve()` in `Handle()` and also return `ReserveSeat`. Do not manually call the operation's `Execute()` before returning it. Either would perform the work twice. Moving the same write into `Provide()` also leaves it outside operation recovery.
:::

## Preserve the right extension boundary

If a custom `ICommandResponseValueHandler` exists only to perform this application side effect, the operation can replace that handler and its typed proxy marker. Remove the unused handler declaration rather than retaining redundant discovery and dependency requirements. Operation values are reserved for Arc's operation phase, not dispatched to ordinary response handlers again.

Keep specialized response-value handlers for framework integrations and return-value interpretation. Keep returned Chronicle events for durable domain facts; do not wrap event appends in operations to bypass the Chronicle append contract. Keep reactors, outboxes, and durable workflows when the work must follow committed facts reliably.

A controller action does not become an operation-aware command by returning an operation object. Use the [model-bound pipeline](../command-pipeline.md), or keep the controller's existing service interaction when its HTTP behavior is the requirement.

## Check scopes and compensation before enabling the new path

An operation-bearing command uses the [supported scope profile](./reference.md#supported-scope-profile): flat execution, explicitly compatible scopes, and at most one deferred commit participant. Audit custom execution scopes and explicit aggregate commits before migrating. Do not label a committing scope as a nonparticipant merely to make the command run.

Review `ReserveSeat.Compensate()` as a domain operation, not a generic delete-on-error callback. The provider must protect ownership, conflicting request-key reuse, and previously finalized work, including when a reservation acknowledgment was lost. If a reversal is not meaningful, omit it and accept that failed execution may leave uncompensated work.

Arc manages failure handling and recovery selection; you do not add a `try/catch` or rollback stack to the command.

## Prove that behavior was preserved

Use [operation specifications](../../testing/command-operations.md) at both boundaries:

1. Call `Handle()` directly and assert the response identifier and operation data. No provider should be needed for this decision spec.
2. Execute through `CommandScenario`, register a controlled provider, and assert the actual provider arguments and caller response.
3. Inject a forward failure and assert the original failure, stopped execution, and eligible compensation—not only an unsuccessful result.
4. Keep validation and authorization specs. Returning an operation does not replace either concern.
5. With Chronicle, [exercise a real commit rejection](../../testing/command-operations-with-chronicle.md) and verify that rejected events do not persist.

Review retry behavior in the injected provider as well. Arc does not automatically retry operations, but an HTTP resilience policy, database execution strategy, or driver may retry inside a provider call. Those policies still need ownership-safe idempotency.

The migration is complete when the response and provider effects are preserved, the decision is independently testable, and the recovery boundary is explicit. For unexpected outcomes, use the [troubleshooting reference](./reference.md#troubleshooting).
