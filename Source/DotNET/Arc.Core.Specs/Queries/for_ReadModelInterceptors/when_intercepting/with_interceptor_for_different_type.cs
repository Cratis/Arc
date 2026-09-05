// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_interceptor_for_different_type : given.a_read_model_interceptors
{
    OtherReadModelInterceptor _otherInterceptor;
    TestReadModel _item;
    IEnumerable<object> _result;

    void Establish()
    {
        _item = new TestReadModel("hello");
        _otherInterceptor = new OtherReadModelInterceptor();

        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([typeof(OtherReadModelInterceptor)]);
        _serviceProvider = Substitute.For<IServiceProvider>();

        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because() => _result = await _interceptors.Intercept(typeof(TestReadModel), [_item], _serviceProvider);

    [Fact] void should_not_resolve_unmatched_interceptor() => _serviceProvider.DidNotReceive().GetService(typeof(OtherReadModelInterceptor));
    [Fact] void should_return_original_item() => _result.ShouldContain(_item);
}
