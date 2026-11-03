// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionServiceScope.when_transferring;

public class after_disposal : Specification
{
    IServiceScope _scope;
    ObservableQuerySubscriptionServiceScope _holder;
    StreamingQuerySubscription _lifetime;
    bool _transferred;

    void Establish()
    {
        _scope = Substitute.For<IServiceScope>();
        _holder = new ObservableQuerySubscriptionServiceScope(_scope);
        _lifetime = new StreamingQuerySubscription(CancellationToken.None);
        _holder.Dispose();
    }

    void Because()
    {
        _transferred = _holder.TryTransferTo(_lifetime);
        _lifetime.Dispose();
    }

    [Fact] void should_not_transfer() => _transferred.ShouldBeFalse();
    [Fact] void should_have_disposed_the_scope_exactly_once() => _scope.Received(1).Dispose();
}
