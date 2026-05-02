// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_multiple_interceptors_for_same_type : given.a_read_model_interceptors
{
    TestReadModelInterceptor _firstInterceptor;
    AnotherTestReadModelInterceptor _secondInterceptor;
    TestReadModel _item;

    void Establish()
    {
        _item = new TestReadModel("hello");
        _firstInterceptor = new TestReadModelInterceptor();
        _secondInterceptor = new AnotherTestReadModelInterceptor();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns(
        [
            typeof(TestReadModelInterceptor),
            typeof(AnotherTestReadModelInterceptor)
        ]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(TestReadModelInterceptor)).Returns(_firstInterceptor);
        _serviceProvider.GetService(typeof(AnotherTestReadModelInterceptor)).Returns(_secondInterceptor);

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => await _interceptors.Intercept(typeof(TestReadModel), [_item], _serviceProvider);

    [Fact] void should_call_first_interceptor() => _firstInterceptor.InterceptedItems.ShouldContain(_item);
    [Fact] void should_call_second_interceptor() => _secondInterceptor.InterceptedItems.ShouldContain(_item);
}
