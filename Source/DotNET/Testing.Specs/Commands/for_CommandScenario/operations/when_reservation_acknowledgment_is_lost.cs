// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations;

public class when_reservation_acknowledgment_is_lost : Specification
{
    CommandScenario<given.ReserveOrderCapacity> _scenario;
    given.ICapacityReservations _reservations;
    Guid _key;
    bool _reserved;
    bool _canceled;
    CommandResult _result;

    void Establish()
    {
        _key = Guid.NewGuid();
        _reservations = Substitute.For<given.ICapacityReservations>();
        _reservations.Reserve(_key, "warehouse", 2, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _reserved = true;
            throw new InvalidCommandOperation("The reservation acknowledgment was lost.");
        });
        _reservations.Cancel(_key, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _canceled = true;
            _reserved = false;
            return Task.CompletedTask;
        });
        _scenario = new();
        _scenario.Services.AddSingleton(_reservations);
    }

    async Task Because() => _result = await _scenario.Execute(new(_key, "warehouse", 2));
    async Task Destroy() => await _scenario.DisposeAsync();

    [Fact] void should_preserve_failure() => _result.ShouldNotBeSuccessful();
    [Fact] void should_release_only_the_owned_reservation() => _reserved.ShouldBeFalse();
    [Fact] void should_actually_call_the_provider_reversal() => _canceled.ShouldBeTrue();
    [Fact] void should_record_compensation_of_the_partial_invocation() => _scenario.ShouldHaveCompensatedOperation<given.ReserveCapacity>();
    [Fact] void should_not_simulate_a_successful_execute() => Catch.Exception(_scenario.ShouldHaveExecutedOperation<given.ReserveCapacity>).ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_report_observed_callback_completion() => _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Completed);
}
