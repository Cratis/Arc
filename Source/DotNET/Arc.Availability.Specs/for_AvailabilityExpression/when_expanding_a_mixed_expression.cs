// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityExpression;

public class when_expanding_a_mixed_expression : Specification
{
    string _result;

    /// <summary>(for A or B) and when X, or with Staff.</summary>
    void Because()
    {
        var catalog = new AvailabilityCatalog().Add(AvailabilityDefinition.FromLines("Staff", AvailabilityExpression.For("Admin")));
        _result = AlternativeText.Of(
            AvailabilityExpression.AnyOf(
                AvailabilityExpression.AllOf(AvailabilityExpression.For("A", "B"), AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Available, "X"))),
                AvailabilityExpression.With("Staff")).Alternatives(catalog));
    }

    [Fact] void should_carry_the_condition_in_each_alternative_it_guards() => _result.ShouldEqual("A+[X]|B+[X]|Admin");
}
