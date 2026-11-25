// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_an_audience_is_switched_on : given.a_development_host
{
    AvailabilityDecision _before;
    AvailabilityDecision _after;

    async Task Because()
    {
        _before = await Decide(typeof(PlusCommand));
        await _development.Apply(AvailabilityOverrides.None with { EnabledAudiences = ["Plus"] });
        _after = await Decide(typeof(PlusCommand));
    }

    [Fact] void should_not_hold_it_beforehand() => _before.IsAvailable.ShouldBeFalse();
    [Fact] void should_open_the_gates_it_satisfies() => _after.IsAvailable.ShouldBeTrue();
}
