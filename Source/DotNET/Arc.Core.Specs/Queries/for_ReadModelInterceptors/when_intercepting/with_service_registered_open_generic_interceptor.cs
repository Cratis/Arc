// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_service_registered_open_generic_interceptor : given.a_read_model_interceptors
{
    ServiceRegisteredGenericReadModelInterceptorState _state;
    TestReadModel _item;
    IEnumerable<object> _result;

    void Establish()
    {
        _item = new TestReadModel("hello");
        _state = new();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([]);

        var services = new ServiceCollection();
        services.AddSingleton(_state);
        services.AddTransient(typeof(IInterceptReadModel<>), typeof(ServiceRegisteredGenericReadModelInterceptor<>));
        _serviceProvider = services.BuildServiceProvider();

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => _result = await _interceptors.Intercept(typeof(TestReadModel), [_item], _serviceProvider);

    [Fact] void should_call_interceptor_with_item() => _state.InterceptedItems.ShouldContain(_item);
    [Fact] void should_call_interceptor_once() => _state.InterceptedItems.Count.ShouldEqual(1);
    [Fact] void should_return_the_intercepted_item() => _result.ShouldContain(_item);
}
