// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_a_cycle : Specification
{
    Exception _error;

    void Because()
    {
        var catalog = new AvailabilityCatalog()
            .Add(AvailabilityDefinition.FromLines("A", AvailabilityExpression.With("B")))
            .Add(AvailabilityDefinition.FromLines("B", AvailabilityExpression.With("A")));
        _error = Catch.Exception(() => AvailabilityExpression.With("A").Alternatives(catalog));
    }

    [Fact] void should_report_the_cycle() => _error.ShouldBeOfExactType<CircularAvailabilityException>();
}
