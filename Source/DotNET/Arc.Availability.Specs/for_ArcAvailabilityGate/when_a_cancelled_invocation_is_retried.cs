// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_cancelled_invocation_is_retried : given.an_invocation_host
{
    Exception _failure;
    AvailabilityDecision _retried;

    protected override ValueTask<object?> ReadSelected(IContextValueResolver resolver, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return base.ReadSelected(resolver, cancellationToken);
    }

    async Task Because()
    {
        var invocation = new AvailabilityInvocation(typeof(UngatedCommand), new { VesselId = "allowed" }, Receipt);
        var gate = (IAvailabilityExecutionGate)_gate;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        try
        {
            await gate.Decide(invocation, _scope.ServiceProvider, cancellation.Token);
        }
        catch (OperationCanceledException exception)
        {
            _failure = exception;
        }

        _retried = await gate.Decide(invocation, _scope.ServiceProvider, CancellationToken.None);
    }

    [Fact] void should_propagate_cancellation() => _failure.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_not_poison_a_later_resolution() => _retried.IsAvailable.ShouldBeTrue();
}
