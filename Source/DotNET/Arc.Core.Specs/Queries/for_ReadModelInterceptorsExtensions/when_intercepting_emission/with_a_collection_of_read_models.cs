// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptorsExtensions.when_intercepting_emission;

/// <summary>
/// A collection query emits an observable of an enumerable of read models. The interceptor must be keyed by the
/// element read model type, not the enumerable type — otherwise no interceptor is found for the enumerable and
/// compliance/PII release is silently skipped for every item.
/// </summary>
public class with_a_collection_of_read_models : given.interceptors
{
    TestReadModel _first;
    TestReadModel _second;
    IEnumerable<TestReadModel> _value;
    object _result;

    void Establish()
    {
        _first = new TestReadModel("one");
        _second = new TestReadModel("two");
        _value = [_first, _second];
    }

    async Task Because() => _result = await _interceptors.InterceptEmission(typeof(IEnumerable<TestReadModel>), _value, _serviceProvider);

    [Fact] void should_intercept_using_the_element_read_model_type() =>
        _interceptors.Received().Intercept(typeof(TestReadModel), Arg.Any<IEnumerable<object>>(), _serviceProvider);
    [Fact] void should_not_intercept_using_the_enumerable_type() =>
        _interceptors.DidNotReceive().Intercept(typeof(IEnumerable<TestReadModel>), Arg.Any<IEnumerable<object>>(), Arg.Any<IServiceProvider>());
    [Fact] void should_intercept_every_item() =>
        _interceptors.Received().Intercept(typeof(TestReadModel), Arg.Is<IEnumerable<object>>(_ => _.Contains(_first) && _.Contains(_second)), _serviceProvider);
    [Fact] void should_return_the_intercepted_collection() => ((IEnumerable<object>)_result).ShouldContain(_first);
}
