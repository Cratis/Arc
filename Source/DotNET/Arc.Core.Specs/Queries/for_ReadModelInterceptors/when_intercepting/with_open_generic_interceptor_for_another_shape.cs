// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_open_generic_interceptor_for_another_shape : given.a_read_model_interceptors
{
    public class ListInterceptor<TReadModel> : IInterceptReadModel<List<TReadModel>>
    {
        public Task<List<TReadModel>> Intercept(List<TReadModel> readModel) => Task.FromResult(readModel);
    }

    TestReadModel _item;
    IEnumerable<object> _items;
    IEnumerable<object> _result;

    void Establish()
    {
        _item = new TestReadModel("hello");
        _items = [_item];

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(ListInterceptor<>)]);
        _serviceProvider = Substitute.For<IServiceProvider>();

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => _result = await _interceptors.Intercept(typeof(TestReadModel), _items, _serviceProvider);

    [Fact] void should_return_the_items_untouched() => _result.ShouldBeSame(_items);
    [Fact] void should_not_create_the_interceptor() => _serviceProvider.DidNotReceive().GetService(typeof(ListInterceptor<TestReadModel>));
}
