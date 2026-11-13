// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_module_and_a_feature_are_gated : given.an_availability_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _audiences.Grant(AudienceLevel.Tenant, "acme", "Preview");

    async Task Because() => _decision = await _gate.Decide(typeof(given.Fleet.Quality.ScoresReadModel), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_close_a_part_whose_feature_needs_another_audience() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Unavailable);
    [Fact] void should_gate_the_part() => _gate.IsGated(typeof(given.Fleet.Quality.ScoresReadModel)).ShouldBeTrue();
}
