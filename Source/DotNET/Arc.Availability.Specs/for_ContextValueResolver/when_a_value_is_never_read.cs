// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_value_is_never_read : Specification
{
    InMemoryReadModelQuery _query;
    ContextValueResolver _request;

    void Because()
    {
        _query = new();
        _request = Requests.For(
            null,
            new ContextValueCatalog().Declare(new ContextValueDeclaration(
                "vessel", new ReadModelContextValueProvider(_query, "VesselByKey", "arguments.vesselId"), ContextScope.Application)));
    }

    [Fact] void should_not_run_the_provider() => _query.Executions.ShouldEqual(0);
    [Fact] void should_not_have_resolved_anything() => _request.ResolvedNames.Count.ShouldEqual(0);
}
