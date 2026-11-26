// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_named_block_does_not_hold : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog().Add(AvailabilityDefinition.FromLines("DataQuality", AvailabilityExpression.For("Plus"), AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Available))))).Evaluate(
            AvailabilityExpression.With("DataQuality"),
            Requests.For(Requests.Holding("Other")));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Unavailable);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(false);
}
