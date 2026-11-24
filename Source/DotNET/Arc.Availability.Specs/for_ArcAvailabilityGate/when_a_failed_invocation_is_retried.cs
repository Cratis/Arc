// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Context;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_failed_invocation_is_retried : given.an_invocation_host
{
    int _attempts;
    Exception _failure;
    AvailabilityDecision _retried;

    protected override ValueTask<object?> ReadSelected(IContextValueResolver resolver, CancellationToken cancellationToken) =>
        Interlocked.Increment(ref _attempts) == 1
            ? throw new InvalidOperationException("temporary provider failure")
            : base.ReadSelected(resolver, cancellationToken);

    async Task Because()
    {
        var invocation = new AvailabilityInvocation(typeof(UngatedCommand), new { VesselId = "allowed" }, Receipt);
        var gate = (IAvailabilityExecutionGate)_gate;
        try
        {
            await gate.Decide(invocation, _scope.ServiceProvider, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _failure = exception;
        }

        _retried = await gate.Decide(invocation, _scope.ServiceProvider, CancellationToken.None);
    }

    [Fact] void should_propagate_the_provider_failure() => _failure.ShouldBeOfExactType<ContextValueProviderFailedException>();
    [Fact] void should_not_cache_the_failure_as_a_denial() => _retried.IsAvailable.ShouldBeTrue();
    [Fact] void should_call_the_provider_again() => _attempts.ShouldEqual(2);
}
