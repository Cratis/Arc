// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_subscriptions_run_concurrently;

public class with_two_subscriptions_over_websocket : given.a_scoped_websocket_container
{
    readonly ConcurrentBag<given.a_scoped_subscription_container.ScopedProbe> _again = [];
    CountdownEvent _gate;
    bool _bothInFlight = true;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _gate = new CountdownEvent(2);
        _performResult = async _ =>
        {
            var context = _httpRequestContextAccessor.Current;
            _gate.Signal();
            if (!await Task.Run(() => _gate.Wait(TimeSpan.FromSeconds(10))))
            {
                _bothInFlight = false;
            }

            _again.Add(context.RequestServices.GetRequiredService<given.a_scoped_subscription_container.ScopedProbe>());
            _signals.Signal();
            return _result();
        };
    }

    async Task Because() => await RunConnection(() => WaitFor(() => _again.Count == 2));

    [Fact] void should_have_both_establishments_in_flight() => _bothInFlight.ShouldBeTrue();
    [Fact] void should_keep_each_subscription_on_its_own_probe() => _again.Distinct().Count().ShouldEqual(2);
    [Fact] void should_resolve_the_same_probe_before_and_after() => _again.OrderBy(_ => _.GetHashCode()).SequenceEqual(_resolvedDuringPerform.OrderBy(_ => _.GetHashCode())).ShouldBeTrue();
    [Fact] void should_dispose_every_probe_exactly_once() => _again.All(_ => _.Disposals == 1).ShouldBeTrue();
}
