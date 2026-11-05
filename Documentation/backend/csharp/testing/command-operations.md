---
title: Cratis Arc command operation and compensation tests
description: Test Cratis Arc operation declarations directly, then use CommandScenario to check provider calls and reverse-order compensation after failure.
---

Test Cratis Arc command operations directly to check what work a handler declares. Use `CommandScenario` to check that Arc executes that work and, after a failure, invokes compensation: the business reversal of work already started.

This lesson follows a seat booking from its returned decision to provider calls and reverse-order recovery. The reservation provider is substituted, so you need no server or Cratis Chronicle package. Arc manages the recovery sequence; neither the command nor the spec needs its own rollback stack.

## Set up the lesson

Create an application library and a separate spec project:

```bash
dotnet new classlib -n SeatBooking
dotnet add SeatBooking package Cratis.Arc.Core
dotnet new xunit -n SeatBooking.Specs
dotnet add SeatBooking.Specs reference SeatBooking/SeatBooking.csproj
dotnet add SeatBooking.Specs package Cratis.Arc.Testing
dotnet add SeatBooking.Specs package Cratis.Specifications.XUnit
dotnet add SeatBooking.Specs package NSubstitute
```

Use matching current Arc package versions. Remove the template's `Class1.cs` and `UnitTest1.cs`, then add the `Reservation.cs`, `ReserveSeat.cs`, `BookSeat.cs`, and `BookSeats.cs` declarations from [implementing an operation](../commands/operations/implementing.md) to `SeatBooking`.

The lesson needs no Chronicle package. Its reservation service is an application interface; the specs supply an NSubstitute implementation. `CommandScenario` does not automatically replace real external dependencies, so do not register a production reservation client in these specs.

## Specify the decision without infrastructure

Create this spec:

```csharp title="for_BookSeat/when_deciding_to_book.cs"
using System;
using Cratis.Specifications;
using SeatBooking;
using Xunit;

namespace SeatBooking.Specs;

public class when_deciding_to_book : Specification
{
    readonly ReservationId _reservationId = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly SeatId _seatId = new("A-12");
    (ReservationId Response, ReserveSeat Operation) _decision;

    void Because() =>
        _decision = new BookSeat(_reservationId, _seatId).Handle();

    [Fact] void should_return_the_reservation_identifier() =>
        _decision.Response.ShouldEqual(_reservationId);

    [Fact] void should_declare_the_requested_reservation() =>
        _decision.Operation.ShouldEqual(new ReserveSeat(_reservationId, _seatId));
}
```

Run it:

```bash
dotnet test SeatBooking.Specs --filter FullyQualifiedName~when_deciding_to_book
```

The two assertions check the returned identifier and the declared reservation. Record value equality lets you compare the operation's inputs directly. Calling `Handle()` does not execute `ReserveSeat.Execute()` or `Compensate()`, so this spec cannot tell you whether the provider is called.

Add decision branches here when you introduce conditional reservations or additional operations. Use fixed inputs rather than the clock or randomly generated data when those would obscure the decision being specified.

## Specify the provider adapter directly

A direct operation spec verifies how its data maps to the provider request:

```csharp title="for_ReserveSeat/when_reserving_directly.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Specifications;
using NSubstitute;
using SeatBooking;
using Xunit;

namespace SeatBooking.Specs;

public class when_reserving_directly : Specification
{
    readonly ReservationId _reservationId = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly SeatId _seatId = new("A-12");
    ISeatReservations _reservations = null!;

    void Establish()
    {
        _reservations = Substitute.For<ISeatReservations>();
        _reservations.Reserve(_reservationId, _seatId, CancellationToken.None)
            .Returns(Task.CompletedTask);
    }

    Task Because() =>
        new ReserveSeat(_reservationId, _seatId)
            .Execute(_reservations, CancellationToken.None);

    [Fact] Task should_reserve_the_requested_seat() =>
        _reservations.Received(1).Reserve(_reservationId, _seatId, CancellationToken.None);

    [Fact] Task should_not_cancel_the_reservation() =>
        _reservations.DidNotReceive().Cancel(Arg.Any<ReservationId>(), Arg.Any<CancellationToken>());
}
```

This checks how `ReserveSeat` maps its inputs to the provider request. It does not check Arc's execution order or automatic compensation; calling an operation directly does not run that orchestration. Use the next scenario to test the connection.

## Execute through CommandScenario

Create a scenario spec using the same command and provider interface:

```csharp title="for_BookSeat/when_booking_through_arc.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeatBooking;
using Xunit;

namespace SeatBooking.Specs;

public class when_booking_through_arc : Specification
{
    readonly CommandScenario<BookSeat> _scenario = new();
    readonly ReservationId _reservationId = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly SeatId _seatId = new("A-12");
    ISeatReservations _reservations = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _reservations = Substitute.For<ISeatReservations>();
        _reservations.Reserve(_reservationId, _seatId, CancellationToken.None)
            .Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_reservations);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new BookSeat(_reservationId, _seatId));

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();

    [Fact] Task should_call_the_provider() =>
        _reservations.Received(1).Reserve(_reservationId, _seatId, CancellationToken.None);

    [Fact] void should_observe_one_started_operation() =>
        _scenario.Operations.Count.ShouldEqual(1);

    [Fact] void should_observe_successful_execution() =>
        _scenario.ShouldHaveExecutedOperation<ReserveSeat>();

    [Fact] void should_not_need_compensation() =>
        _result.Recovery!.Status.ShouldEqual(CommandRecoveryStatus.NotNeeded);

    [Fact] void should_return_only_the_caller_response() =>
        ((CommandResult<ReservationId>)_result).Response.ShouldEqual(_reservationId);

    void Destroy() => _scenario.Dispose();
}
```

Register substitutes before the first execution, when `CommandScenario` builds its service provider. The scenario runs the command pipeline and operation invokers rather than recording declarations in a stub. Its `Operations` snapshot contains invocations that entered execution during the latest `Execute`, so you can distinguish planned work from work Arc actually started.

The cast uses the actual response type. An operation failure may produce a result without a usable typed response, so check success before consuming responses in application code.

## Prove compensation after a partial failure

Now make the second reservation fail. The first two operations enter `Execute()`, but the third must never start. Arc should attempt cancellation of the second reservation first, because the provider may have accepted it before returning a failure.

```csharp title="for_BookSeats/when_the_second_reservation_fails.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeatBooking;
using Xunit;

namespace SeatBooking.Specs;

public class ReservationProviderUnavailable()
    : Exception("Reservation provider unavailable.");

public class when_the_second_reservation_fails : Specification
{
    readonly CommandScenario<BookSeats> _scenario = new();
    readonly ReservationId _first = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    readonly ReservationId _second = new(Guid.Parse("b52a8554-fab3-4050-af81-554d105d9b9d"));
    readonly ReservationId _third = new(Guid.Parse("0aeb4b2f-0515-44fe-8e79-7c8f304d2c65"));
    ISeatReservations _reservations = null!;
    CommandResult _result = null!;

    void Establish()
    {
        _reservations = Substitute.For<ISeatReservations>();
        _reservations.Reserve(Arg.Any<ReservationId>(), Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _reservations.Reserve(_second, Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ReservationProviderUnavailable()));
        _reservations.Cancel(Arg.Any<ReservationId>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_reservations);
    }

    async Task Because() =>
        _result = await _scenario.Execute(new BookSeats(
        [
            new SeatReservation(_first, new SeatId("A-12")),
            new SeatReservation(_second, new SeatId("A-13")),
            new SeatReservation(_third, new SeatId("A-14"))
        ]));

    [Fact] void should_preserve_the_command_failure() =>
        _result.ShouldNotBeSuccessful();

    [Fact] void should_preserve_the_original_error() =>
        _result.ExceptionMessages.ShouldContain("Reservation provider unavailable.");

    [Fact] void should_record_only_the_two_entered_invocations() =>
        _scenario.Operations.Count.ShouldEqual(2);

    [Fact] void should_reserve_the_first_two_seats_in_order() => Received.InOrder(() =>
    {
        _ = _reservations.Reserve(_first, new SeatId("A-12"), Arg.Any<CancellationToken>());
        _ = _reservations.Reserve(_second, new SeatId("A-13"), Arg.Any<CancellationToken>());
    });

    [Fact] void should_observe_the_first_execution_completing() =>
        _scenario.Operations[0].ExecutionCompleted.ShouldBeTrue();

    [Fact] void should_observe_the_second_execution_failing() =>
        _scenario.Operations[1].ExecutionCompleted.ShouldBeFalse();

    [Fact] Task should_not_start_the_third_reservation() =>
        _reservations.DidNotReceive().Reserve(_third, Arg.Any<SeatId>(), Arg.Any<CancellationToken>());

    [Fact] void should_compensate_in_reverse_order() => Received.InOrder(() =>
    {
        _ = _reservations.Cancel(_second, Arg.Any<CancellationToken>());
        _ = _reservations.Cancel(_first, Arg.Any<CancellationToken>());
    });

    [Fact] void should_observe_both_compensations() =>
        _result.Recovery!.CompensatedCount.ShouldEqual(2);

    [Fact] void should_report_completed_recovery_without_success() =>
        _result.Recovery!.Status.ShouldEqual(CommandRecoveryStatus.Completed);

    void Destroy() => _scenario.Dispose();
}
```

The specific error and provider calls establish where execution stopped. The skipped third reservation checks that Arc started no later work, while the cancellation order checks recovery of both entered operations. A lone `ShouldNotBeSuccessful()` could also pass if a missing dependency prevented every operation from running.

`Completed` means the required compensation callbacks returned, not that the command succeeded. A provider-backed test must still check durable cancellation and whether a delayed reservation can appear afterward.

## Add cancellation and rejection cases

`CommandScenario.Execute(command, cancellationToken)` forwards the token into the real pipeline. Use it to test request cancellation after an operation starts. The compensator receives an independent cleanup token, not the canceled request token.

For a cancellation spec, have a controlled collaborator cancel the forward token, then assert that compensation was invoked with a token that was not already canceled. Do not sleep or race a real network request to manufacture cancellation. The [execution reference](../commands/operations/reference.md#cancellation-and-recovery-budget) explains the cooperative cleanup budget.

Also call `Validate(command)` in a fresh scenario and assert that neither provider method was called. `Validate` does not invoke `Provide()`, `Handle()`, operations, compensation, or execution scopes. It is not a substitute for testing execution-time or commit-time rejection.

When testing a specific validation rule, assert its message or reason in addition to proving the provider was untouched. See [precise scenario assertions](./command-scenario.md#commandresult-assertion-helpers).

## Run the lesson and choose the next boundary

Run all the specs:

```bash
dotnet test SeatBooking.Specs
```

The direct specs check what work is declared and how the operation calls its provider. The scenario specs check that Arc connects those pieces and recovers in reverse order after a failure. Keep new decision branches in the direct specs; add scenario cases when the risk concerns execution or recovery.

Continue with [operation failure cases](./command-operation-failures.md) for runnable cancellation, validation-rejection, missing-compensator, and failed-compensation specs. Backend `Recovery` and `OperationOutcomes` remain available to these tests; they are not serialized to frontend clients.

At the provider boundary, also test a repeated request after a reservation was finalized: a failed new attempt must not cancel the earlier successful reservation. Verify conflicting key reuse, tenant/owner isolation, and delayed creation after cancellation. Substitutes cannot establish these external guarantees.

If the command also returns an event, [test recovery after a real Chronicle constraint rejection](./command-operations-with-chronicle.md). That example seeds a conflict and checks both non-commit and compensation. It covers a known rejection; already-committed or unknown outcomes need different handling, not blind reversal.

Continue with the [operation reference](../commands/operations/reference.md) for exact supported signatures, flat-command limits, and recovery observations, or the [testing overview](./index.md) to select provider-backed integration tests.
