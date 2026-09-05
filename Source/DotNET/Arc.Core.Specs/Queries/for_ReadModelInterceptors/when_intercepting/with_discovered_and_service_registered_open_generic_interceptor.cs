// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_discovered_and_service_registered_open_generic_interceptor : given.a_read_model_interceptors
{
    ServiceRegisteredGenericReadModelInterceptorState _state;
    TestReadModel _item;

    void Establish()
    {
        _item = new TestReadModel("hello");
        _state = new();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(ServiceRegisteredGenericReadModelInterceptor<>)]);

        var services = new ServiceCollection();
        services.AddSingleton(_state);
        services.AddTransient(typeof(IInterceptReadModel<>), typeof(ServiceRegisteredGenericReadModelInterceptor<>));
        _serviceProvider = services.BuildServiceProvider();

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => await _interceptors.Intercept(typeof(TestReadModel), [_item], _serviceProvider);

    [Fact] void should_only_call_interceptor_once() => _state.InterceptedItems.Count.ShouldEqual(1);
}
