// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_part_is_gated_with_a_typed_block_that_says_not : given.a_typed_availability_host
{
    AvailabilityDecision _plus_and_incident;

    async Task Establish()
    {
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Incident");
    }

    async Task Because() => _plus_and_incident = await _gate.Decide(typeof(given.TypedDeclarations.ScoresCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_close_while_the_incident_audience_is_held() => _plus_and_incident.Verdict.ShouldEqual(AvailabilityVerdict.Unavailable);
    [Fact] void should_name_the_not_as_the_cause() => _plus_and_incident.Causes.ShouldContain("not Incident");
}
