// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_ArcAvailabilityGate;

public class when_asking_for_the_gated_parts : given.an_availability_host
{
    IReadOnlyCollection<Type> _parts;

    void Because() => _parts = _gate.GatedParts;

    [Fact] void should_include_parts_gated_with_attributes() => _parts.ShouldContain(typeof(given.an_availability_host.PlusCommand));
    [Fact] void should_include_parts_in_gated_namespaces() => _parts.ShouldContain(typeof(given.Fleet.Billing.InvoiceCommand), typeof(given.Fleet.Quality.ScoresReadModel));
    [Fact] void should_not_include_ungated_parts() => _parts.ShouldNotContain(typeof(given.an_availability_host.UngatedCommand));
}
