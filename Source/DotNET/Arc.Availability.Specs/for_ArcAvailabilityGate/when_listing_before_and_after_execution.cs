// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_listing_before_and_after_execution : given.an_invocation_host
{
    AvailabilityDecision _before;
    AvailabilityDecision _execution;
    AvailabilityDecision _after;

    async Task Because()
    {
        _before = await _gate.Decide(typeof(UngatedCommand), _scope.ServiceProvider, CancellationToken.None);
        _execution = await Decide(new { VesselId = "allowed" });
        _after = await _gate.Decide(typeof(UngatedCommand), _scope.ServiceProvider, CancellationToken.None);
    }

    [Fact] void should_fail_closed_without_an_instance() => _before.Verdict.ShouldEqual(AvailabilityVerdict.Unknown);
    [Fact] void should_not_poison_execution_with_the_listing_cache() => _execution.IsAvailable.ShouldBeTrue();
    [Fact] void should_not_let_execution_widen_listing() => _after.Verdict.ShouldEqual(AvailabilityVerdict.Unknown);
    [Fact] void should_keep_separate_resolvers() => _calls.ShouldEqual(2);
}
