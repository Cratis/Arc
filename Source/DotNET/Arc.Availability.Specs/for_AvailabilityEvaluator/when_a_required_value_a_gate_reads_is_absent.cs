// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_required_value_a_gate_reads_is_absent : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
        AvailabilityExpression.When(new ContextComparison("vessel.hasReported", ComparisonOperator.IsTrue)),
        Requests.For(
            Requests.Holding(),
            new ContextValueCatalog().Declare(new ContextValueDeclaration(
                "vessel", new HostContextValueProvider(new InMemoryHostContextValues(), "vessel"), ContextScope.Application)))));

    [Fact] void should_refuse_the_request_rather_than_deny() => _error.ShouldBeOfExactType<RequiredContextValueAbsentException>();
}
