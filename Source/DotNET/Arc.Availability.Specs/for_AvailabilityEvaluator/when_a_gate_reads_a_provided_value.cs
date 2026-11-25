// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_AvailabilityEvaluator;

public class when_a_gate_reads_a_provided_value : Specification
{
    AvailabilityResult _result;

    async Task Because()
    {
        var query = new InMemoryReadModelQuery();
        query.Add("VesselByKey", "v1", new Dictionary<string, object?> { ["commercialPackage"] = "Plus" });
        var request = Requests.For(
            Requests.Holding(),
            new ContextValueCatalog().Declare(new ContextValueDeclaration(
                "vessel", new ReadModelContextValueProvider(query, "VesselByKey", "arguments.vesselId"), ContextScope.Application)),
            new Dictionary<string, object?> { ["arguments"] = new Dictionary<string, object?> { ["vesselId"] = "v1" } });
        _result = await new AvailabilityEvaluator(new AvailabilityCatalog()).Evaluate(
            AvailabilityExpression.When(new ContextComparison("vessel.commercialPackage", ComparisonOperator.In, "Plus", "Pro")),
            request);
    }

    [Fact] void should_be_available() => _result.IsAvailable.ShouldBeTrue();
}
