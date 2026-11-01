// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQuerySubscriptionServiceScope.when_transferring;

public class twice : Specification
{
    IServiceScope _scope;
    ObservableQuerySubscriptionServiceScope _holder;
    StreamingQuerySubscription _lifetime;
    bool _first;
    bool _second;

    void Establish()
    {
        _scope = Substitute.For<IServiceScope>();
        _holder = new ObservableQuerySubscriptionServiceScope(_scope);
        _lifetime = new StreamingQuerySubscription(CancellationToken.None);
    }

    void Because()
    {
        _first = _holder.TryTransferTo(_lifetime);
        _second = _holder.TryTransferTo(_lifetime);
        _lifetime.Dispose();
    }

    [Fact] void should_transfer_the_first_time() => _first.ShouldBeTrue();
    [Fact] void should_refuse_the_second_time() => _second.ShouldBeFalse();
    [Fact] void should_dispose_the_scope_exactly_once() => _scope.Received(1).Dispose();
}
