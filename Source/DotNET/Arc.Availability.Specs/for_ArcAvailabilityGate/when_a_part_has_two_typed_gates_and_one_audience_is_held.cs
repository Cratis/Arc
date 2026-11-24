// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_part_has_two_typed_gates_and_one_audience_is_held : given.a_typed_availability_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");

    async Task Because() => _decision = await _gate.Decide(typeof(given.TypedDeclarations.BothAudiencesCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_stay_closed_because_every_line_has_to_hold() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Unavailable);
}
