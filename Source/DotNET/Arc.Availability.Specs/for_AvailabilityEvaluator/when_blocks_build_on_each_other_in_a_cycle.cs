// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_blocks_build_on_each_other_in_a_cycle : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () =>
        await new AvailabilityEvaluator(
            new AvailabilityCatalog()
                .Add(AvailabilityDefinition.FromLines("A", AvailabilityExpression.With("B")))
                .Add(AvailabilityDefinition.FromLines("B", AvailabilityExpression.With("A")))).Evaluate(
            AvailabilityExpression.With("A"),
            Requests.For(Requests.Holding())));

    [Fact] void should_fail_as_a_model_error() => _error.ShouldBeOfExactType<CircularAvailabilityException>();
}
