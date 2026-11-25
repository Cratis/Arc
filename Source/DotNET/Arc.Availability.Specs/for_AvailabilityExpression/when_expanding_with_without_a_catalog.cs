// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_with_without_a_catalog : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => AvailabilityExpression.With("Staff").Alternatives());

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
