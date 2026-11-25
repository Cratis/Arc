// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_key_cannot_be_resolved : Specification
{
    InMemoryReadModelQuery _query;
    ContextValueOutcome _outcome;

    async Task Because()
    {
        _query = new();
        _outcome = await Requests.For(
            null,
            new ContextValueCatalog().Declare(new ContextValueDeclaration(
                "vessel", new ReadModelContextValueProvider(_query, "Q", "arguments.vesselId"), ContextScope.Application, Required: false)))
            .Resolve("vessel");
    }

    [Fact] void should_be_absent() => _outcome.IsPresent.ShouldBeFalse();
    [Fact] void should_not_run_the_query() => _query.Executions.ShouldEqual(0);
}
