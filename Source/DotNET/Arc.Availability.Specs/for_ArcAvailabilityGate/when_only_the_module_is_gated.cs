// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability.Audiences;

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_only_the_module_is_gated : given.an_availability_host
{
    AvailabilityDecision _decision;

    async Task Establish() => await _audiences.Grant(AudienceLevel.Tenant, "acme", "Preview");

    async Task Because() => _decision = await _gate.Decide(typeof(given.Fleet.Billing.InvoiceCommand), _scope.ServiceProvider, CancellationToken.None);

    [Fact] void should_open_a_part_of_the_module_for_its_audience() => _decision.IsAvailable.ShouldBeTrue();
}
