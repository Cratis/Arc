// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_subscriptions_run_concurrently;

public class with_different_tenants_over_sse : given.a_scoped_subscription_container
{
    const int Rounds = 20;
    readonly ConcurrentBag<(int Round, string Tenant, ScopedProbe Probe)> _observed = [];
    CountdownEvent _gate;
    int _round;
    bool _bothInFlight = true;

    void Establish() => _performResult = async _ =>
    {
        var context = _httpRequestContextAccessor.Current;
        var round = _round;
        _gate.Signal();

        // Both establishments are in flight at once before either resolves again.
        if (!await Task.Run(() => _gate.Wait(TimeSpan.FromSeconds(10))))
        {
            _bothInFlight = false;
        }

        var again = context.RequestServices.GetRequiredService<ScopedProbe>();
        _observed.Add((round, context.Headers["x-tenant"], again));
        return _result();
    };

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        for (_round = 0; _round < Rounds; _round++)
        {
            _gate = new CountdownEvent(2);
            var a = CreateSubscribeContext($"a-{_round}");
            a.Headers.Returns(new Dictionary<string, string> { ["x-tenant"] = "tenant-a" });
            var b = CreateSubscribeContext($"b-{_round}");
            b.Headers.Returns(new Dictionary<string, string> { ["x-tenant"] = "tenant-b" });
            await Task.WhenAll(_hub.HandleSSESubscribe(a), _hub.HandleSSESubscribe(b));
            _gate.Dispose();
        }

        await _connectionCancellation.CancelAsync();
        await connectionTask;
    }

    [Fact] void should_have_both_establishments_in_flight_each_round() => _bothInFlight.ShouldBeTrue();
    [Fact] void should_use_a_different_probe_for_every_subscription() => _observed.Select(_ => _.Probe).Distinct().Count().ShouldEqual(Rounds * 2);
    [Fact] void should_keep_each_tenant_on_its_own_probe() => _observed.All(_ => _.Probe.Tenant == _.Tenant).ShouldBeTrue();
    [Fact] void should_dispose_every_probe_exactly_once() => _observed.All(_ => _.Probe.Disposals == 1).ShouldBeTrue();
}
