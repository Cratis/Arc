// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_condition_fails : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () =>
        await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.When(new FixedCondition(AvailabilityOutcome.Available, fails: true)),
            Requests.For(Requests.Holding())));

    [Fact] void should_surface_the_failure_instead_of_a_denial() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
