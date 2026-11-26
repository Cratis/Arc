// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_and_has_an_unknown_and_an_available_part : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.AllOf(AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Unknown)), AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Available))),
            Requests.For(Requests.Holding()));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unknown);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(false);
}
