---
title: Implement a command operation
description: Return a reservation operation from a pure command decision and let Arc execute and compensate it without application error-handling boilerplate.
---

**Goal:** a command returns the seat reservation it intends to make, while Arc performs the reservation and manages an optional cancellation if later execution fails safely before commitment.

Use this recipe in an application configured with `Cratis.Arc.Core` or `Cratis.Arc`. Chronicle is optional. You need an application reservation provider; the [testing lesson](../../testing/command-operations.md) uses a substitute so you can exercise the same contract without an external service.

## Define the operation's inputs and provider

Add the following application declarations. Keep them in the same `SeatBooking` namespace; shared concepts normally live in separate files. `ReservationId` is an ordinary strongly typed request identifier, not a Chronicle event-source identity.

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

`Reserve` and `Cancel` belong to your provider, not Arc. Register its implementation as `ISeatReservations` through the host's normal dependency injection. No operation executor or response-value-handler registration is needed.

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

Arc discovers the operation's method shape and supplies the scoped service and cancellation token. The operation carries only business data; its methods receive infrastructure when invoked. The normal build includes diagnostics for invalid declarations and generated invocation support.

There is no `try/catch` or success flag. Arc decides whether to compensate, including when this operation's `Execute()` threw after possibly changing the remote system. The provider's cancellation protocol must accommodate that uncertainty.

Omit `Compensate()` when there is no meaningful reversal. Arc still executes and reports the operation, but cannot claim to have reversed it after a failure.

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

Arc recognizes the operation as server-consumed work. It invokes `Execute()` and exposes only the `ReservationId` response. Your generated TypeScript command has no `ReserveSeat` response object or compensation method to call.

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

Awaiting `Book()` waits for operation execution, scope completion, and any eligible compensation. On success, `IsSuccess` is true and `Response` contains the reservation identifier. On failure, Arc returns an unsuccessful result and does not require the caller to implement a rollback stack.

**Calling `Handle()` directly does not execute the operation.** Use that direct call for a decision spec, not as a substitute for `ICommandPipeline` in production.

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

An empty input produces an empty batch. Membership is materialized once, and operations execute sequentially in the given order. Different operation types can share the same `CommandOperations` batch. Use a distinct ownership key for each logical reservation.

Ordinary arrays and enumerables remain valid response data; the explicit wrapper tells Arc that you mean executable work. For a fixed set of different operations, a tuple is also supported.

## Check the recovery boundary before deploying

This initial operation contract supports flat, sequential commands and compatible execution scopes, with at most one deferred commit participant. Nested operation-bearing commands, receipt-returning execution methods, automatic retries, and durable recovery are not part of this contract.

With Chronicle, return events for deferred completion. Do not explicitly commit an aggregate and then expect a later operation failure to undo that commit. An unknown or already-committed business outcome suppresses automatic reversal; [the reference](./reference.md#commit-and-recovery) explains the reporting.

:::caution[Cancellation is not proof that nothing happened]
A timeout or disconnect may arrive after the provider reserved the seat. A compensator that merely checks for absence can race a still-running request. The provider must offer a cancellation or reconciliation protocol appropriate to that behavior. Arc cannot turn two external calls into one atomic transaction.
:::

Next, [test the declaration, execution, and compensation](../../testing/command-operations.md) through direct specs and `CommandScenario` before connecting a production provider.
