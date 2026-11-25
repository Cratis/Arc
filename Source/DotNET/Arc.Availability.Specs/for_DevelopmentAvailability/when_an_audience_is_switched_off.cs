// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_an_audience_is_switched_off : given.a_development_host
{
    AvailabilityDecision _before;
    AvailabilityDecision _after;

    async Task Establish() => await _audiences.Grant(AudienceLevel.Tenant, "acme", "Plus");

    async Task Because()
    {
        _before = await Decide(typeof(PlusCommand));
        await _development.Apply(AvailabilityOverrides.None with { DisabledAudiences = ["Plus"] });
        _after = await Decide(typeof(PlusCommand));
    }

    [Fact] void should_hold_it_beforehand() => _before.IsAvailable.ShouldBeTrue();
    [Fact] void should_close_the_gates_that_need_it() => _after.IsAvailable.ShouldBeFalse();
}
