// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_parentheses_change_the_grouping : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.AllOf(AvailabilityExpression.AnyOf(AvailabilityExpression.For("Plus"), AvailabilityExpression.For("Preview")), AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Available))),
            Requests.For(Requests.Holding("Preview")));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Available);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(true);
}
