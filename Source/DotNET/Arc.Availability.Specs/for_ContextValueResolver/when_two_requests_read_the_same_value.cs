// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_two_requests_read_the_same_value : Specification
{
    InMemoryReadModelQuery _query;

    async Task Because()
    {
        _query = new();
        _query.Add("Q", "k", "row");
        var catalog = new ContextValueCatalog().Declare(new ContextValueDeclaration(
            "thing", new ReadModelContextValueProvider(_query, "Q", "key"), ContextScope.Application));
        var ambient = new Dictionary<string, object?> { ["key"] = "k" };
        await Requests.For(null, catalog, ambient).Resolve("thing");
        await Requests.For(null, catalog, ambient).Resolve("thing");
    }

    [Fact] void should_not_share_a_cache_across_requests() => _query.Executions.ShouldEqual(2);
}
