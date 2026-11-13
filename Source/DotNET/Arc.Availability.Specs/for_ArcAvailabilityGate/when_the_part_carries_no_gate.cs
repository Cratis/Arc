// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_the_part_carries_no_gate : given.an_availability_host
{
    AvailabilityDecision _decision;

    async Task Because() => _decision = await _gate.Decide(typeof(given.an_availability_host.UngatedCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_be_available() => _decision.IsAvailable.ShouldBeTrue();
    [Fact] void should_not_be_gated() => _gate.IsGated(typeof(given.an_availability_host.UngatedCommand)).ShouldBeFalse();
}
