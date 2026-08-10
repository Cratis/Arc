// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

/// <summary>
/// A replaying subject hands its buffered value to the observer from inside Subscribe, so the guard denies before the
/// subscribe frame has tracked anything — and the teardown the denial runs finds nothing. The connection would then go
/// on reporting a live subscription that will never serve a byte, and hold its service scope until the whole
/// connection ends, so the registration and the release have to be balanced here and not deferred.
/// </summary>
public class and_guard_denies_the_first_replayed_emission : given.a_guarded_websocket_connection
{
    readonly ConcurrentQueue<SubscriptionScopeProbe> _probes = new();

    ReplaySubject<IEnumerable<string>> _replaySubject;
    bool _scopeReleasedWhileConnected;

    void Establish()
    {
        _replaySubject = new ReplaySubject<IEnumerable<string>>(1);
        _replaySubject.OnNext(["buffered-before-anyone-subscribed"]);
        _streamingData = _replaySubject;
    }

    async Task Because() => await RunConnection(() =>
    {
        // Read while the connection is still open: the connection's own teardown disposes everything it tracks, so
        // asserting afterwards could not tell a prompt release from one that waited for the client to go away.
        _scopeReleasedWhileConnected = _probes.Count == 1 && _probes.Single().IsDisposed;
        return Task.CompletedTask;
    });

    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_register_the_subscription() => _healthTracker.Received(1).RegisterSubscription(Arg.Any<string>(), "WebSocket", Arg.Any<QuerySubscriptionMetadata>());
    [Fact] void should_unregister_the_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_release_the_subscription_scope_without_waiting_for_the_connection() => _scopeReleasedWhileConnected.ShouldBeTrue();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(_probes);
        services.AddScoped<SubscriptionScopeProbe>();
        guardTypes.Add(typeof(DenyingScopeProbeGuard));
    }

    public class SubscriptionScopeProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    public class DenyingScopeProbeGuard(ConcurrentQueue<SubscriptionScopeProbe> probes, SubscriptionScopeProbe probe) : IGuardObservableQueryEmission
    {
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            probes.Enqueue(probe);
            return Task.FromResult(ObservableQueryEmissionVerdict.DenyAndTerminate);
        }
    }
}
