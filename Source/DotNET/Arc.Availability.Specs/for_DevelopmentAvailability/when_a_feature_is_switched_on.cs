// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_DevelopmentAvailability;

public class when_a_feature_is_switched_on : given.a_development_host
{
    AvailabilityDecision _gated;
    AvailabilityDecision _beforehand;

    async Task Because()
    {
        _beforehand = await Decide(typeof(given.Fleet.Billing.InvoiceCommand));
        await _development.Apply(AvailabilityOverrides.None with { EnabledPaths = [FeatureOf(typeof(given.Fleet.Billing.InvoiceCommand))] });
        _gated = await Decide(typeof(given.Fleet.Billing.InvoiceCommand));
    }

    [Fact] void should_have_been_unavailable_beforehand() => _beforehand.IsAvailable.ShouldBeFalse();
    [Fact] void should_make_the_part_available_whatever_its_gates_say() => _gated.IsAvailable.ShouldBeTrue();
}
