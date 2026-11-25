// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_required_value_is_absent : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await Requests.For(
        null,
        new ContextValueCatalog().Declare(new ContextValueDeclaration(
            "vessel", new HostContextValueProvider(new InMemoryHostContextValues(), "vessel"), ContextScope.Application)))
        .Resolve("vessel"));

    [Fact] void should_refuse_with_a_clear_reason() => _error.ShouldBeOfExactType<RequiredContextValueAbsentException>();
    [Fact] void should_name_the_value() => ((RequiredContextValueAbsentException)_error).Name.ShouldEqual("vessel");
}
