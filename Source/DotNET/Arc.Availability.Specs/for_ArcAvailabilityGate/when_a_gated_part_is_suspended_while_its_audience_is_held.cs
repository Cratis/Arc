// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Suspensions;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_gated_part_is_suspended_while_its_audience_is_held : given.a_suspension_host
{
    AvailabilityDecision _decision;

    async Task Establish()
    {
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");
        await _suspensions.Suspend(AudienceLevel.Tenant, "acme", PartPaths.For(typeof(PlusCommand)));
    }

    async Task Because() => _decision = await Decide(typeof(PlusCommand));

    [Fact] void should_be_suspended_not_available() => _decision.Verdict.ShouldEqual(AvailabilityVerdict.Suspended);
}
