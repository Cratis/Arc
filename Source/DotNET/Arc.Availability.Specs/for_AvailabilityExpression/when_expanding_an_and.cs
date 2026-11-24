// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_an_and : Specification
{
    string _result;

    void Because() => _result = AlternativeText.Of(
        AvailabilityExpression.AllOf(AvailabilityExpression.For("A"), AvailabilityExpression.For("B"), AvailabilityExpression.For("A")).Alternatives());

    [Fact] void should_be_one_alternative_with_every_audience_once() => _result.ShouldEqual("A+B");
}
