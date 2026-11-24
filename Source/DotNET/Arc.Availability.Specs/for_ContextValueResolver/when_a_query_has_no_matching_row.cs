// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_query_has_no_matching_row : Specification
{
    ContextValueOutcome _outcome;

    async Task Because() => _outcome = await Requests.For(
        null,
        new ContextValueCatalog().Declare(new ContextValueDeclaration(
            "vessel", new ReadModelContextValueProvider(new InMemoryReadModelQuery(), "Q", "key"), ContextScope.Application, Required: false)),
        new Dictionary<string, object?> { ["key"] = "k" })
        .Resolve("vessel");

    [Fact] void should_be_absent() => _outcome.IsPresent.ShouldBeFalse();
}
