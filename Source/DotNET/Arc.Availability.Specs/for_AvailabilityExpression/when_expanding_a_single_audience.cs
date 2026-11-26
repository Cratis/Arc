// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_a_single_audience : Specification
{
    string _result;

    void Because() => _result = AlternativeText.Of(AvailabilityExpression.For("Admin").Alternatives());

    [Fact] void should_be_one_alternative_with_the_audience() => _result.ShouldEqual("Admin");
}
