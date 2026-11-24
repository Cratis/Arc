// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityCatalog;

public class when_validating_a_use_of_an_undefined_block : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new AvailabilityCatalog().Add(AvailabilityDefinition.FromLines("A", AvailabilityExpression.With("Missing"))).Validate());

    [Fact] void should_throw_an_undefined_error() => _error.ShouldBeOfExactType<UndefinedAvailabilityException>();
}
