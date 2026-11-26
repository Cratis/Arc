// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Context.Providers;

namespace Cratis.Arc.Availability.for_ContextValueResolver;

public class when_a_failed_value_is_read_again : Specification
{
    int _runs;
    Exception _error;

    async Task Because()
    {
        var request = Requests.For(
            null,
            new ContextValueCatalog().Declare(new ContextValueDeclaration(
                "vessel",
                new CodeContextValueProvider((_, _) =>
                {
                    _runs++;
                    throw new TimeoutException();
                }),
                ContextScope.Application)));
        await Catch.Exception(async () => await request.Resolve("vessel"));
        _error = await Catch.Exception(async () => await request.Resolve("vessel"));
    }

    [Fact] void should_fail_again_without_rerunning_the_provider() => _runs.ShouldEqual(1);
    [Fact] void should_still_be_an_error() => _error.ShouldBeOfExactType<ContextValueProviderFailedException>();
}
