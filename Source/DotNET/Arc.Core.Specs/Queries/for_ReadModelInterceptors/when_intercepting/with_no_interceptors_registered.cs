// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ReadModelInterceptors.when_intercepting;

public class with_no_interceptors_registered : Specification
{
    ReadModelInterceptors _interceptors;
    IServiceProvider _serviceProvider;
    object _item;
    bool _threw;
    IEnumerable<object> _result;

    void Establish()
    {
        _item = new object();
        var types = Substitute.For<ITypes>();
        types.FindMultiple(typeof(IInterceptReadModel<>)).Returns([]);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _interceptors = new ReadModelInterceptors(types);
    }

    async Task Because()
    {
        _threw = false;
        try
        {
            _result = await _interceptors.Intercept(typeof(object), [_item], _serviceProvider);
        }
        catch
        {
            _threw = true;
        }
    }

    [Fact] void should_not_throw() => _threw.ShouldBeFalse();
    [Fact] void should_return_original_item() => _result.ShouldContain(_item);
}
