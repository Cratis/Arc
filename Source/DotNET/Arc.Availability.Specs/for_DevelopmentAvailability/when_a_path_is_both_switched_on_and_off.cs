// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_a_path_is_both_switched_on_and_off : given.a_development_host
{
    AvailabilityDecision _decision;

    async Task Because()
    {
        var path = PathOf(typeof(given.Fleet.Billing.InvoiceCommand));
        await _development.Apply(AvailabilityOverrides.None with { EnabledPaths = [path], DisabledPaths = [path] });
        _decision = await Decide(typeof(given.Fleet.Billing.InvoiceCommand));
    }

    [Fact] void should_switch_it_off() => _decision.IsAvailable.ShouldBeFalse();
}
