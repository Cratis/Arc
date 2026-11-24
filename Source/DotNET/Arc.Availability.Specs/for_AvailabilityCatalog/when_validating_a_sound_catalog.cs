// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityCatalog;

public class when_validating_a_sound_catalog : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new AvailabilityCatalog()
            .Add(AvailabilityDefinition.FromLines("Inner", AvailabilityExpression.For("Plus")))
            .Add(AvailabilityDefinition.FromLines("Outer", AvailabilityExpression.With("Inner")))
            .Validate());

    [Fact] void should_not_throw() => _error.ShouldBeNull();
}
