// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_block_is_not_defined : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () =>
        await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.With("Missing"),
            Requests.For(Requests.Holding())));

    [Fact] void should_fail_as_a_model_error() => _error.ShouldBeOfExactType<UndefinedAvailabilityException>();
}
