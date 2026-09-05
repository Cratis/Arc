// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptorsExtensions.when_intercepting_emission;

/// <summary>
/// A string is an enumerable of characters, but a string-returning query is a single value, not a collection — it
/// must be intercepted as one value rather than enumerated character by character.
/// </summary>
public class with_a_string_value : given.interceptors
{
    object _result;

    async Task Because() => _result = await _interceptors.InterceptEmission(typeof(string), "abc", _serviceProvider);

    [Fact] void should_intercept_the_string_as_a_single_value() =>
        _interceptors.Received().Intercept(typeof(string), Arg.Is<IEnumerable<object>>(_ => _.Count() == 1), _serviceProvider);
    [Fact] void should_return_the_string() => _result.ShouldEqual("abc");
}
