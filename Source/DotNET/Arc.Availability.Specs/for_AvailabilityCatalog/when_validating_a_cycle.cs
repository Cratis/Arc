// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityCatalog;

public class when_validating_a_cycle : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new AvailabilityCatalog()
            .Add(AvailabilityDefinition.FromLines("A", AvailabilityExpression.AnyOf(AvailabilityExpression.With("B"))))
            .Add(AvailabilityDefinition.FromLines("B", AvailabilityExpression.With("A")))
            .Validate());

    [Fact] void should_throw_a_cycle_error() => _error.ShouldBeOfExactType<CircularAvailabilityException>();
}
