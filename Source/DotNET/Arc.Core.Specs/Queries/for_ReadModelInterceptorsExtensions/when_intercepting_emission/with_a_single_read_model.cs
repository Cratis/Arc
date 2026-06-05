// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptorsExtensions.when_intercepting_emission;

public class with_a_single_read_model : given.interceptors
{
    TestReadModel _value;
    object _result;

    void Establish() => _value = new TestReadModel("hello");

    async Task Because() => _result = await _interceptors.InterceptEmission(typeof(TestReadModel), _value, _serviceProvider);

    [Fact] void should_intercept_using_the_read_model_type() =>
        _interceptors.Received().Intercept(typeof(TestReadModel), Arg.Any<IEnumerable<object>>(), _serviceProvider);
    [Fact] void should_return_the_single_intercepted_value() => _result.ShouldEqual(_value);
}
