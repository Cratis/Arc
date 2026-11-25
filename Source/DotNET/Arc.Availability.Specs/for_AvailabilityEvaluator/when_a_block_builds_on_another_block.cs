// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_block_builds_on_another_block : Specification
{
    AvailabilityResult _result;

    async Task Because() =>
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog().Add(AvailabilityDefinition.FromLines("Inner", AvailabilityExpression.For("Plus"))).Add(AvailabilityDefinition.FromLines("Outer", AvailabilityExpression.With("Inner")))).Evaluate(
            AvailabilityExpression.With("Outer"),
            Requests.For(Requests.Holding("Plus")));

    [Fact] void should_have_the_outcome() => _result.Outcome.ShouldEqual(AvailabilityOutcome.Available);
    [Fact] void should_only_open_when_available() => _result.IsAvailable.ShouldEqual(true);
}
