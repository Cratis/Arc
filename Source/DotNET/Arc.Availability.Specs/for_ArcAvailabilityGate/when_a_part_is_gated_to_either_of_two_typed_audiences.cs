// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_part_is_gated_to_either_of_two_typed_audiences : given.a_typed_availability_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _audiences.Grant(AudienceLevel.Tenant, "acme", "Preview");

    async Task Because() => _decision = await _gate.Decide(typeof(given.TypedDeclarations.EitherAudienceCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_open_when_either_is_held() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Available);
}
