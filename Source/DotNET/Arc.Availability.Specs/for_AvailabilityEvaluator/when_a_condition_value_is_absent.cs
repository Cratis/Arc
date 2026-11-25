// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_condition_value_is_absent : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.When(new ContextComparison("vessel.hasReported", ComparisonOperator.IsTrue)),
            Requests.For(Requests.Holding()));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unknown);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(false);
}
