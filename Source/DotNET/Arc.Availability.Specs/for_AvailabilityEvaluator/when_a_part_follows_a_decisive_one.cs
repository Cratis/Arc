// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_part_follows_a_decisive_one : Specification
{
    FixedCondition _later;
    AvailabilityResult _result;

    async Task Establish()
    {
        _later = new FixedCondition(AvailabilityOutcome.Available, fails: true);
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.AllOf(AvailabilityExpression.For("Plus"), AvailabilityExpression.When(_later)),
            Requests.For(Requests.Holding()));
    }

    [Fact] void should_deny() => _result.IsAvailable.ShouldBeFalse();
    [Fact] void should_not_evaluate_the_later_part() => _later.Evaluations.ShouldEqual(0);
}
