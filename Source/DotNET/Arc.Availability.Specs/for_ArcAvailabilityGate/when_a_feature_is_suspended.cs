// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_a_feature_is_suspended : given.a_suspension_host
{
    AvailabilityDecision _billing;
    AvailabilityDecision _quality;
    AvailabilityDecision _elsewhere;

    async Task Establish()
    {
        await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");
        await _suspensions.Suspend(AudienceLevel.Tenant, "acme", FeaturePath);
    }

    async Task Because()
    {
        _billing = await Decide(Invoice);
        _quality = await Decide(Scores);
        _elsewhere = await Decide(Ungated);
    }

    [Fact] void should_suspend_the_slice_of_the_feature_that_is_billing() => _billing.Verdict.ShouldEqual(AvailabilityVerdict.Suspended);
    [Fact] void should_suspend_the_slice_of_the_feature_that_is_quality() => _quality.Verdict.ShouldEqual(AvailabilityVerdict.Suspended);
    [Fact] void should_not_suspend_a_part_outside_the_feature() => _elsewhere.IsAvailable.ShouldBeTrue();
}
