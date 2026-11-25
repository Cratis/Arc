// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_an_or : Specification
{
    string _result;

    void Because() => _result = AlternativeText.Of(AvailabilityExpression.For("A", "B", "C").Alternatives());

    [Fact] void should_be_one_alternative_per_audience_in_authored_order() => _result.ShouldEqual("A|B|C");
}
