---
title: Implement a command operation in Cratis Arc
description: Return a seat reservation operation from a Cratis Arc command, execute it through the pipeline, and define compensation within its recovery limits.
---

Implement a Cratis Arc command operation when your command can describe a service write before making it. Here, `BookSeat.Handle()` returns a seat reservation declaration. Arc makes the provider call through the command pipeline and attempts cancellation if the command fails and its commit outcome permits recovery.

Start with an application configured with `Cratis.Arc.Core` or `Cratis.Arc`. Cratis Chronicle is optional. You also need a reservation provider that can honor the ownership and cancellation contract below. The [testing lesson](../../testing/command-operations.md) uses a substitute to exercise the Arc pipeline without an external service.

## Define the operation's inputs and provider

First, define the request data and the interface your reservation provider will implement. Keep these declarations in the same `SeatBooking` namespace; shared concepts normally live in separate files. `ReservationId` identifies the reservation request. It is not a Chronicle event-source identity.

```csharp title="Reservation.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Concepts;

namespace SeatBooking;

public record ReservationId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static readonly ReservationId NotSet = new(Guid.Empty);
    public static implicit operator ReservationId(Guid value) => new(value);
}

public record SeatId(string Value) : ConceptAs<string>(Value)
{
    public static readonly SeatId NotSet = new(string.Empty);
    public static implicit operator SeatId(string value) => new(value);
}

public interface ISeatReservations
{
    Task Reserve(
        ReservationId reservationId,
        SeatId seatId,
        CancellationToken cancellationToken);

    Task Cancel(ReservationId reservationId, CancellationToken cancellationToken);
}
```

`Reserve` and `Cancel` are application methods, not Arc APIs. Register your provider as `ISeatReservations` through the host's normal dependency injection. Arc will supply it to the operation methods, so you do not register a separate operation executor or response value handler.

The provider must enforce ownership and make cancellation meaningful for an attempted reservation. Use a request identifier within the correct tenant and owner's scope. Bind that identifier immutably to its intended request, reject conflicting reuse, and protect previously finalized reservations from a later attempt's compensation. A request identifier is neither authorization to cancel somebody else's reservation nor proof that the current invocation created an existing reservation.

If a duplicate request returns an earlier successful reservation, cancellation must not destroy that finalized success when some other operation in the new command attempt fails. Verify this contract with the production provider. Arc supplies orchestration, not a universal idempotency or reservation-ownership protocol.

## Pair execution with its business reversal

Add the operation in the same project:

```csharp title="ReserveSeat.cs"
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;

namespace SeatBooking;

public sealed record ReserveSeat(ReservationId ReservationId, SeatId SeatId)
    : ICommandOperation
{
    public Task Execute(
        ISeatReservations reservations,
        CancellationToken cancellationToken) =>
        reservations.Reserve(ReservationId, SeatId, cancellationToken);

    public Task Compensate(
        ISeatReservations reservations,
        CancellationToken cancellationToken) =>
        reservations.Cancel(ReservationId, cancellationToken);
}
```

`ReserveSeat` carries the identifiers needed for both calls, without capturing a provider instance. Arc discovers its methods and resolves their required services from the command's service provider before any operation starts. It supplies the cancellation token when invoking each method. The normal build includes diagnostics for invalid declarations and generated invocation support.

`Compensate()` calls the provider with the same reservation identifier as `Execute()`. That connection matters when a reservation succeeds remotely but its acknowledgment is lost: Arc includes the throwing operation in eligible recovery, even though `Execute()` did not complete locally. You write the cancellation behavior; the pipeline tracks the attempt without a success flag or `try/catch` in the operation.

Omit `Compensate()` when there is no meaningful reversal. Arc still executes the operation, but records that compensation is unavailable if recovery is needed.

## Return the declaration from a command

Add a model-bound command:

```csharp title="BookSeat.cs"
using Cratis.Arc.Commands.ModelBound;

namespace SeatBooking;

[Command]
public record BookSeat(ReservationId ReservationId, SeatId SeatId)
{
    public (ReservationId Response, ReserveSeat Operation) Handle() =>
        (ReservationId, new ReserveSeat(ReservationId, SeatId));
}
```

`Handle()` constructs an operation; it does not reserve the seat. The same inputs produce the same returned values, without I/O, a clock, or random identity generation inside the decision. The caller supplies the request identifier.

Arc consumes `ReserveSeat` on the server and invokes its `Execute()` method. On success, the caller receives only the `ReservationId` response. The generated TypeScript command does not expose the operation or a compensation method. Operation methods return no response data, so this example uses an identifier known before execution.

Apply your normal [validation](../validation.md) and [authorization](../model-bound/authorization.md). Operations do not replace either, and a direct call to `Handle()` does not run those pipeline checks.

## Execute through the normal pipeline

This caller uses the shared declarations above. Register the application provider before resolving it from an Arc-configured service scope:

```csharp title="BookingRequests.cs"
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;

namespace SeatBooking;

public class BookingRequests(ICommandPipeline pipeline)
{
    public Task<CommandResult<ReservationId>> Book(
        ReservationId reservationId,
        SeatId seatId,
        CancellationToken cancellationToken) =>
        pipeline.Execute<ReservationId>(
            new BookSeat(reservationId, seatId),
            cancellationToken);
}
```

Awaiting `Book()` waits for operation execution, scope completion, and any eligible compensation. On success, `IsSuccess` is true and `Response` contains the reservation identifier. On failure, Arc returns an unsuccessful result and clears the response. A failed result alone does not tell you whether the provider changed anything; backend callers can inspect the [recovery observations](./reference.md#backend-observations).

Calling `Handle()` directly only constructs the declaration. Use that call for a decision spec and `ICommandPipeline` when you want the reservation to execute.

## Return zero or many operations

An optional singular operation can be null:

```csharp title="BookSeatIfNeeded.cs"
using Cratis.Arc.Commands.ModelBound;

namespace SeatBooking;

[Command]
public record BookSeatIfNeeded(
    ReservationId ReservationId,
    SeatId SeatId,
    bool ShouldReserve)
{
    public ReserveSeat? Handle() =>
        ShouldReserve ? new ReserveSeat(ReservationId, SeatId) : null;
}
```

Arc skips the absent operation. This command has no client response.

For a variable number of operations, use `CommandOperations` rather than returning a raw array or enumerable:

```csharp title="BookSeats.cs"
using System.Collections.Generic;
using System.Linq;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;

namespace SeatBooking;

public record SeatReservation(ReservationId ReservationId, SeatId SeatId);

[Command]
public record BookSeats(IReadOnlyList<SeatReservation> Reservations)
{
    public CommandOperations Handle() =>
    [
        .. Reservations.Select(reservation =>
            new ReserveSeat(reservation.ReservationId, reservation.SeatId))
    ];
}
```

An empty input produces an empty batch. `CommandOperations` copies the declarations once and preserves their order. Arc validates all declarations and resolves the services required by both execution and compensation before starting the first operation. It then executes them sequentially, stopping if one fails. Different operation types can share the same batch; use a distinct ownership key for each logical reservation.

Use the wrapper for operation collections: a bare array or enumerable of operation declarations is rejected. Ordinary arrays and enumerables of response data remain valid responses. A tuple also works for a fixed set of different operations.

## Check the recovery boundary before deploying

Operations support flat, sequential commands. Do not execute nested commands from `Execute()` or `Compensate()`, return receipts from those methods, or rely on automatic retries or recovery after a process crash. Execution scopes, which surround the command with integration work such as transaction completion, must explicitly support operations. At most one may coordinate a deferred business commit. Check custom scopes against the [supported scope profile](./reference.md#supported-scope-profile).

With Chronicle, return events for deferred completion. Arc enrolls those events before operations execute and completes the event transaction afterward. Do not explicitly commit an aggregate and expect a later operation failure to undo that commit. Arc permits compensation when there is no commit participant or the participant reports that it did not commit; it suppresses reversal for committed, unknown, or mixed outcomes. The [commit and recovery reference](./reference.md#commit-and-recovery) explains the reporting.

When recovery is eligible, Arc attempts compensation in reverse order, including the operation whose execution threw. A compensation failure does not replace the original command failure or prevent the next eligible callback. Compensation receives a separate token with a shared thirty-second budget by default. That timeout is cooperative: a provider that ignores cancellation can run longer. See [cancellation and recovery budget](./reference.md#cancellation-and-recovery-budget) to configure it.

:::caution[Cancellation is not proof that nothing happened]
A timeout or disconnect may arrive after the provider reserved the seat. A compensator that merely checks for absence can race a still-running request. The provider must offer a cancellation or reconciliation protocol appropriate to that behavior. Arc cannot turn two external calls into one atomic transaction.
:::

Next, [test the declaration, execution, and compensation](../../testing/command-operations.md) through direct specs and `CommandScenario` before connecting a production provider.
