// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Context;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_parallel_invocations_resolve_in_one_scope : given.an_invocation_host
{
    readonly TaskCompletionSource _bothReading = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _readers;
    AvailabilityDecision[] _decisions;

    protected override async ValueTask<object?> ReadSelected(IContextValueResolver resolver, CancellationToken cancellationToken)
    {
        if (Interlocked.Increment(ref _readers) == 2)
        {
            _bothReading.TrySetResult();
        }

        await _bothReading.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        return await base.ReadSelected(resolver, cancellationToken);
    }

    async Task Because() => _decisions = await Task.WhenAll(
        Decide(new { VesselId = "allowed" }).AsTask(),
        Decide(new { VesselId = "denied" }).AsTask());

    [Fact] void should_allow_only_the_matching_invocation() => _decisions.Select(_ => _.IsAvailable).ShouldEqual([true, false]);
    [Fact] void should_resolve_each_invocation_independently() => _calls.ShouldEqual(2);
}
