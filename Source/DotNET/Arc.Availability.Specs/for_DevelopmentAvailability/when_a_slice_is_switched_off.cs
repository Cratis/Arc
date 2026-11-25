// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_a_slice_is_switched_off : given.a_development_host
{
    AvailabilityDecision _switchedOff;
    AvailabilityDecision _other;

    async Task Because()
    {
        await _development.Apply(AvailabilityOverrides.None with { DisabledPaths = [PathOf(typeof(given.Fleet.Quality.ScoresReadModel))] });
        _switchedOff = await Decide(typeof(given.Fleet.Quality.ScoresReadModel));
        _other = await Decide(typeof(given.Fleet.Billing.InvoiceCommand));
    }

    [Fact] void should_not_make_the_part_available() => _switchedOff.IsAvailable.ShouldBeFalse();
    [Fact] void should_say_why() => _switchedOff.Causes.ShouldContainOnly(ArcAvailabilityGate.SwitchedOff);
    [Fact] void should_leave_another_slice_to_its_own_gates() => _other.Causes.ShouldNotContain(ArcAvailabilityGate.SwitchedOff);
}
