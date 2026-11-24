// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_nested_parentheses : Specification
{
    string _result;

    /// <summary>(A or B) and (C or (D and E)).</summary>
    void Because() => _result = AlternativeText.Of(
        AvailabilityExpression.AllOf(
            AvailabilityExpression.For("A", "B"),
            AvailabilityExpression.AnyOf(
                AvailabilityExpression.For("C"),
                AvailabilityExpression.AllOf(AvailabilityExpression.For("D"), AvailabilityExpression.For("E")))).Alternatives());

    [Fact] void should_distribute_and_over_or() => _result.ShouldEqual("A+C|A+D+E|B+C|B+D+E");
}
