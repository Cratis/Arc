// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_provider_fails : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(async () => await Requests.For(
        null,
        new ContextValueCatalog().Declare(new ContextValueDeclaration(
            "vessel",
            new CodeContextValueProvider((_, _) => throw new TimeoutException("store down")),
            ContextScope.Application,
            Required: false)))
        .Resolve("vessel"));

    [Fact] void should_fail_the_request_with_an_error() => _error.ShouldBeOfExactType<ContextValueProviderFailedException>();
    [Fact] void should_keep_the_cause() => _error.InnerException.ShouldBeOfExactType<TimeoutException>();
}
