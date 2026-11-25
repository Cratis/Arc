// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_part_carries_no_gate : given.an_availability_host
{
    AvailabilityDecision _decision;

    async Task Because() => _decision = await _gate.Decide(typeof(given.an_availability_host.UngatedCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_be_available() => _decision.IsAvailable.ShouldBeTrue();
    [Fact] void should_still_be_decided_because_any_part_can_be_suspended() => _gate.IsGated(typeof(given.an_availability_host.UngatedCommand)).ShouldBeTrue();
    [Fact] void should_not_be_listed_as_a_gated_part() => _gate.GatedParts.ShouldNotContain(typeof(given.an_availability_host.UngatedCommand));
}
