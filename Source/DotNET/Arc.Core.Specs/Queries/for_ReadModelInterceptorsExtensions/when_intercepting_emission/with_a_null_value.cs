// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptorsExtensions.when_intercepting_emission;

public class with_a_null_value : given.interceptors
{
    object _result;

    async Task Because() => _result = await _interceptors.InterceptEmission(typeof(TestReadModel), null, _serviceProvider);

    [Fact] void should_return_null() => _result.ShouldBeNull();
    [Fact] void should_not_invoke_the_interceptors() =>
        _interceptors.DidNotReceive().Intercept(Arg.Any<Type>(), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>());
}
