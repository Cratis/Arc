// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionServiceScope.when_disposing;

public class twice : Specification
{
    IServiceScope _scope;
    ObservableQuerySubscriptionServiceScope _holder;

    void Establish()
    {
        _scope = Substitute.For<IServiceScope>();
        _holder = new ObservableQuerySubscriptionServiceScope(_scope);
    }

    void Because()
    {
        _holder.Dispose();
        _holder.Dispose();
    }

    [Fact] void should_dispose_the_scope_exactly_once() => _scope.Received(1).Dispose();
}
