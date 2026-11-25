// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_reading_a_value_twice_in_one_request : Specification
{
    InMemoryReadModelQuery _query;
    ContextValueOutcome _first;
    ContextValueOutcome _second;

    async Task Because()
    {
        _query = new();
        _query.Add("VesselByKey", "v1", new Dictionary<string, object?> { ["name"] = "Aurora" });
        var catalog = new ContextValueCatalog().Declare(new ContextValueDeclaration(
            "vessel", new ReadModelContextValueProvider(_query, "VesselByKey", "arguments.vesselId"), ContextScope.Application));
        var request = Requests.For(
            null,
            catalog,
            new Dictionary<string, object?> { ["arguments"] = new Dictionary<string, object?> { ["vesselId"] = "v1" } });
        _first = await request.Resolve("vessel.name");
        _second = await request.Resolve("$context.vessel.name");
    }

    [Fact] void should_resolve_the_value() => _first.Value.ShouldEqual("Aurora");
    [Fact] void should_give_the_same_value_again() => _second.Value.ShouldEqual("Aurora");
    [Fact] void should_run_the_provider_once() => _query.Executions.ShouldEqual(1);
}
