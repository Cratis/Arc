// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionServiceScope.when_transferring;

public class to_a_live_lifetime : Specification
{
    IServiceScope _scope;
    ObservableQuerySubscriptionServiceScope _holder;
    StreamingQuerySubscription _lifetime;
    bool _transferred;
    int _disposalsBeforeLifetimeDisposed;

    void Establish()
    {
        _scope = Substitute.For<IServiceScope>();
        _holder = new ObservableQuerySubscriptionServiceScope(_scope);
        _lifetime = new StreamingQuerySubscription(CancellationToken.None);
    }

    void Because()
    {
        _transferred = _holder.TryTransferTo(_lifetime);
        _disposalsBeforeLifetimeDisposed = _scope.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IDisposable.Dispose));
        _lifetime.Dispose();
        _holder.Dispose();
    }

    [Fact] void should_transfer() => _transferred.ShouldBeTrue();
    [Fact] void should_not_dispose_before_the_lifetime_is_disposed() => _disposalsBeforeLifetimeDisposed.ShouldEqual(0);
    [Fact] void should_dispose_exactly_once() => _scope.Received(1).Dispose();
}
