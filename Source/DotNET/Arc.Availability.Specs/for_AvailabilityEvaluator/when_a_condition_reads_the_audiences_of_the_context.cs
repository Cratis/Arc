// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_condition_reads_the_audiences_of_the_context : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.When(new ContextComparison("$context.audiences.matching", ComparisonOperator.IsTrue)),
            Requests.For(Requests.Holding("Plus")));

    [Fact] void should_not_treat_a_non_boolean_as_true() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unavailable);
}
