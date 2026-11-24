// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_an_optional_value_is_absent : Specification
{
    ContextValueOutcome _outcome;

    async Task Because() => _outcome = await Requests.For(
        null,
        new ContextValueCatalog().Declare(new ContextValueDeclaration(
            "vessel", new HostContextValueProvider(new InMemoryHostContextValues(), "vessel"), ContextScope.Application, Required: false)))
        .Resolve("vessel");

    [Fact] void should_be_absent() => _outcome.IsPresent.ShouldBeFalse();
}
