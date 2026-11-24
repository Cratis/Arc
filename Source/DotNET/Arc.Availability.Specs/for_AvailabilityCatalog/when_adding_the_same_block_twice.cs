// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityCatalog;

public class when_adding_the_same_block_twice : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() =>
        new AvailabilityCatalog()
            .Add(AvailabilityDefinition.FromLines("A", AvailabilityExpression.For("Plus")))
            .Add(AvailabilityDefinition.FromLines("A", AvailabilityExpression.For("Plus"))));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
