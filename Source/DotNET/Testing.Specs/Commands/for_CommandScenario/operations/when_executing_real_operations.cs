// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.Commands.for_CommandScenario.operations;

public class when_executing_real_operations : Specification
{
    CommandScenario<given.ReserveOrderCapacity> _scenario;
    given.ICapacityReservations _reservations;
    Guid _key;
    CancellationTokenSource _cancellation;
    CommandResult _result;

    void Establish()
    {
        _key = Guid.NewGuid();
        _cancellation = new();
        _reservations = Substitute.For<given.ICapacityReservations>();
        _scenario = new();
        _scenario.Services.AddSingleton(_reservations);
    }

    async Task Because() => _result = await _scenario.Execute(new(_key, "warehouse", 2), _cancellation.Token);
    async Task Destroy()
    {
        await _scenario.DisposeAsync();
        _cancellation.Dispose();
    }

    [Fact] void should_succeed() => _result.ShouldBeSuccessful();
    [Fact] void should_execute_with_injected_fake_and_forwarded_cancellation() => _reservations.Received(1).Reserve(_key, "warehouse", 2, _cancellation.Token);
    [Fact] void should_offer_truthful_scenario_assertions() => _scenario.ShouldHaveExecutedOperation<given.ReserveCapacity>();
    [Fact] void should_offer_result_assertions_too() => _result.ShouldHaveExecutedOperation<given.ReserveCapacity>();
    [Fact] void should_record_the_observed_execution() => _scenario.Operations.Single().ExecutionCompleted.ShouldBeTrue();
    [Fact] void should_not_claim_compensation_happened() => Catch.Exception(_scenario.ShouldHaveCompensatedOperation<given.ReserveCapacity>).ShouldBeOfExactType<CommandResultAssertionException>();
    [Fact] void should_not_claim_zero_invocations() => Catch.Exception(_scenario.ShouldHaveNoOperationInvocations).ShouldBeOfExactType<CommandResultAssertionException>();
}
