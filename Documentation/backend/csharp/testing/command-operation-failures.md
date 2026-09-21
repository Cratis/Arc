---
title: Specify command operation failure cases
description: Test cancellation, execution-path validation rejection, missing compensation, and failed compensation through the real CommandScenario pipeline.
---

**Goal:** add focused regression specs for recovery boundaries that a successful booking test cannot cover. These recipes extend [testing command operations](./command-operations.md) and reuse its `SeatBooking` application declarations and spec packages.

Each case runs the real `CommandScenario` pipeline with a controlled provider. No command or operation implements a test-only error-handling path.

## Share the scenario setup

Add this reusable specification context. The base `Establish()` registers the provider before a derived case configures its specific failure. `Destroy()` disposes the scenario.

```csharp title="FailureCases/given/a_booking_scenario.cs"
using System;
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SeatBooking;

namespace SeatBooking.Specs.FailureCases.given;

public abstract class a_booking_scenario<TCommand> : Specification
{
    protected readonly CommandScenario<TCommand> _scenario = new();
    protected readonly ReservationId _first = new(Guid.Parse("757660b8-55ac-4c1a-89cb-8151c8fd3562"));
    protected readonly ReservationId _second = new(Guid.Parse("b52a8554-fab3-4050-af81-554d105d9b9d"));
    protected readonly ReservationId _third = new(Guid.Parse("0aeb4b2f-0515-44fe-8e79-7c8f304d2c65"));
    protected ISeatReservations _reservations = null!;
    protected CommandResult _result = null!;

    void Establish()
    {
        _reservations = Substitute.For<ISeatReservations>();
        _reservations.Reserve(Arg.Any<ReservationId>(), Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _reservations.Cancel(Arg.Any<ReservationId>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _scenario.Services.AddSingleton(_reservations);
    }

    void Destroy() => _scenario.Dispose();
}
```

The failure types below give the fault-injection cases precise messages to assert:

```csharp title="FailureCases/Failures.cs"
using System;

namespace SeatBooking.Specs.FailureCases;

public class ForwardFailure() : Exception("Reserve failed.");
public class CompensationFailure() : Exception("Cancel failed.");
```

## Cancel during execution without canceling cleanup

Cancel the forward token from inside the controlled provider call. No delay or real network race is needed:

```csharp title="FailureCases/when_execution_is_canceled.cs"
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Specifications;
using NSubstitute;
using SeatBooking;
using SeatBooking.Specs.FailureCases.given;
using Xunit;

namespace SeatBooking.Specs.FailureCases;

public class when_execution_is_canceled : a_booking_scenario<BookSeats>
{
    readonly CancellationTokenSource _cancellation = new();

    void Establish() =>
        _reservations.Reserve(_first, Arg.Any<SeatId>(), _cancellation.Token)
            .Returns(async _ =>
            {
                await _cancellation.CancelAsync();
                _cancellation.Token.ThrowIfCancellationRequested();
            });

    async Task Because() =>
        _result = await _scenario.Execute(
            new BookSeats(
            [
                new SeatReservation(_first, new SeatId("A-12")),
                new SeatReservation(_second, new SeatId("A-13"))
            ]),
            _cancellation.Token);

    [Fact] void should_keep_the_command_unsuccessful() =>
        _result.IsSuccess.ShouldBeFalse();

    [Fact] Task should_not_start_the_next_operation() =>
        _reservations.DidNotReceive().Reserve(_second, Arg.Any<SeatId>(), Arg.Any<CancellationToken>());

    [Fact] Task should_compensate_with_a_separate_uncanceled_token() =>
        _reservations.Received(1).Cancel(_first,
            Arg.Is<CancellationToken>(token => !token.IsCancellationRequested && token != _cancellation.Token));

    [Fact] void should_observe_the_compensation() =>
        _result.Recovery!.CompensatedCount.ShouldEqual(1);

    void Destroy() => _cancellation.Dispose();
}
```

This proves that request cancellation does not immediately cancel compensation. It does not prove a hard cleanup deadline: compensators must cooperate with the separate recovery token.

## Reject input during Execute before provider work

Define a validator for the existing `BookSeat` command and execute invalid input through the pipeline:

```csharp title="FailureCases/when_input_is_rejected.cs"
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Specifications;
using FluentValidation;
using NSubstitute;
using SeatBooking;
using SeatBooking.Specs.FailureCases.given;
using Xunit;

namespace SeatBooking.Specs.FailureCases;

public class BookSeatValidator : CommandValidator<BookSeat>
{
    public BookSeatValidator() => RuleFor(command => command.SeatId)
        .NotEqual(SeatId.NotSet)
        .WithMessage("Choose a seat.");
}

public class when_input_is_rejected : a_booking_scenario<BookSeat>
{
    async Task Because() =>
        _result = await _scenario.Execute(new BookSeat(_first, SeatId.NotSet));

    [Fact] void should_reject_the_specific_input() =>
        _result.ShouldHaveValidationErrorFor("Choose a seat.");

    [Fact] void should_have_no_entered_operations() =>
        _scenario.Operations.Count.ShouldEqual(0);

    [Fact] Task should_not_reserve() =>
        _reservations.DidNotReceive().Reserve(Arg.Any<ReservationId>(), Arg.Any<SeatId>(), Arg.Any<CancellationToken>());

    [Fact] Task should_not_compensate_unstarted_work() =>
        _reservations.DidNotReceive().Cancel(Arg.Any<ReservationId>(), Arg.Any<CancellationToken>());
}
```

The specific validation message distinguishes the expected rejection from a missing provider or unrelated failure. Unlike a `Validate()` call alone, this exercises the rejection path of `Execute()`.

## Make unavailable compensation observable

An operation can intentionally omit `Compensate()`. This test-only command combines such an operation with the ordinary `ReserveSeat` operation:

```csharp title="FailureCases/when_compensation_is_unavailable.cs"
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Specifications;
using NSubstitute;
using SeatBooking;
using SeatBooking.Specs.FailureCases.given;
using Xunit;

namespace SeatBooking.Specs.FailureCases;

public record ReserveWithoutCompensation(ReservationId ReservationId, SeatId SeatId) : ICommandOperation
{
    public Task Execute(ISeatReservations reservations, CancellationToken cancellationToken) =>
        reservations.Reserve(ReservationId, SeatId, cancellationToken);
}

[Command]
public record TryBookingWithoutFullRecovery(ReservationId First, ReservationId Second)
{
    public CommandOperations Handle() =>
    [
        new ReserveWithoutCompensation(First, new SeatId("A-12")),
        new ReserveSeat(Second, new SeatId("A-13"))
    ];
}

public class when_compensation_is_unavailable : a_booking_scenario<TryBookingWithoutFullRecovery>
{
    void Establish() =>
        _reservations.Reserve(_second, Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForwardFailure()));

    async Task Because() =>
        _result = await _scenario.Execute(new TryBookingWithoutFullRecovery(_first, _second));

    [Fact] void should_report_incomplete_recovery() =>
        _result.Recovery!.Status.ShouldEqual(CommandRecoveryStatus.Incomplete);

    [Fact] void should_identify_the_missing_compensator() =>
        _scenario.Operations[0].Compensation.ShouldEqual(CommandOperationCompensation.NotAvailable);

    [Fact] void should_report_one_uncompensated_invocation() =>
        _result.Recovery!.UncompensatedCount.ShouldEqual(1);

    [Fact] Task should_still_compensate_the_other_operation() =>
        _reservations.Received(1).Cancel(_second, Arg.Any<CancellationToken>());
}
```

The example deliberately supplies no reversal for the first operation. Its purpose is to prove reporting—not to recommend omitting a necessary cancellation in a production booking flow.

## Continue recovery after one compensator fails

Fail the third execution and the second compensator. The first compensator must still run, and the original execution error must remain visible:

```csharp title="FailureCases/when_a_compensator_fails.cs"
using System.Threading;
using System.Threading.Tasks;
using Cratis.Arc.Commands;
using Cratis.Specifications;
using NSubstitute;
using SeatBooking;
using SeatBooking.Specs.FailureCases.given;
using Xunit;

namespace SeatBooking.Specs.FailureCases;

public class when_a_compensator_fails : a_booking_scenario<BookSeats>
{
    void Establish()
    {
        _reservations.Reserve(_third, Arg.Any<SeatId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForwardFailure()));
        _reservations.Cancel(_second, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new CompensationFailure()));
    }

    async Task Because() =>
        _result = await _scenario.Execute(new BookSeats(
        [
            new SeatReservation(_first, new SeatId("A-12")),
            new SeatReservation(_second, new SeatId("A-13")),
            new SeatReservation(_third, new SeatId("A-14"))
        ]));

    [Fact] void should_preserve_the_original_failure() =>
        _result.ExceptionMessages.ShouldContain("Reserve failed.");

    [Fact] void should_keep_the_command_unsuccessful() =>
        _result.IsSuccess.ShouldBeFalse();

    [Fact] void should_record_the_failed_compensation() =>
        _scenario.Operations[1].Compensation.ShouldEqual(CommandOperationCompensation.Failed);

    [Fact] void should_observe_two_successful_compensations() =>
        _result.Recovery!.CompensatedCount.ShouldEqual(2);

    [Fact] void should_observe_one_failed_compensation() =>
        _result.Recovery!.FailedCompensationCount.ShouldEqual(1);

    [Fact] void should_attempt_all_three_compensators_in_reverse_order() => Received.InOrder(() =>
    {
        _ = _reservations.Cancel(_third, Arg.Any<CancellationToken>());
        _ = _reservations.Cancel(_second, Arg.Any<CancellationToken>());
        _ = _reservations.Cancel(_first, Arg.Any<CancellationToken>());
    });
}
```

Run the failure cases:

```bash
dotnet test SeatBooking.Specs --filter FullyQualifiedName~FailureCases
```

These specs deliberately inject faults into collaborators; application operations still contain only their forward work and optional business reversal. For supported commit profiles and uncertain outcomes, continue with the [operation execution reference](../commands/operations/reference.md#commit-and-recovery).
