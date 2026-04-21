// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_matching_interceptor : given.a_read_model_interceptors
{
    TestReadModelInterceptor _interceptorInstance;
    TestReadModel _item;

    void Establish()
    {
        _item = new TestReadModel("hello");
        _interceptorInstance = new TestReadModelInterceptor();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(TestReadModelInterceptor)]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(TestReadModelInterceptor)).Returns(_interceptorInstance);

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => await _interceptors.Intercept(typeof(TestReadModel), _item, _serviceProvider);

    [Fact] void should_call_interceptor_with_item() => _interceptorInstance.InterceptedItems.ShouldContain(_item);
    [Fact] void should_call_interceptor_once() => _interceptorInstance.InterceptedItems.Count.ShouldEqual(1);
}
