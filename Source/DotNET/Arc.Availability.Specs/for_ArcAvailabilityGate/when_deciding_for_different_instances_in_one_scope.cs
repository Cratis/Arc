// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_deciding_for_different_instances_in_one_scope : given.an_invocation_host
{
    AvailabilityDecision _first;
    AvailabilityDecision _second;

    async Task Because()
    {
        _first = await Decide(new { VesselId = "allowed" });
        _second = await Decide(new Dictionary<string, object> { ["vesselId"] = "denied" });
    }

    [Fact] void should_allow_the_matching_instance() => _first.IsAvailable.ShouldBeTrue();
    [Fact] void should_not_reuse_its_value_for_the_next_instance_at_the_same_receipt() => _second.IsAvailable.ShouldBeFalse();
    [Fact] void should_resolve_once_for_each_instance() => _calls.ShouldEqual(2);
}
