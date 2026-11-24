// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_only_when : Specification
{
    string _result;

    void Because() => _result = AlternativeText.Of(
        AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Available, "vessel.commercialPackage = Pro")).Alternatives());

    [Fact] void should_be_one_alternative_with_only_the_condition_text() => _result.ShouldEqual("[vessel.commercialPackage = Pro]");
}
