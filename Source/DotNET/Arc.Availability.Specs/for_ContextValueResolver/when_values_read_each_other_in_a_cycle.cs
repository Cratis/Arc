// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_values_read_each_other_in_a_cycle : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await Requests.For(
        null,
        new ContextValueCatalog()
            .Declare(new ContextValueDeclaration("a", new ExpressionContextValueProvider("b"), ContextScope.Application))
            .Declare(new ContextValueDeclaration("b", new ExpressionContextValueProvider("a"), ContextScope.Application)))
        .Resolve("a"));

    [Fact] void should_detect_the_cycle() => _error.ShouldBeOfExactType<ContextValueCycleException>();
}
