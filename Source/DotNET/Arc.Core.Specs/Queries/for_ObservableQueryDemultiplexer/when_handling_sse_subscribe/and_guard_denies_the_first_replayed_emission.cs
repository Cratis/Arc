// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// A replaying subject hands its buffered value to the observer from inside Subscribe, so the guard denies — and the
/// denial tears the subscription down — before the subscribe POST has anything tracked to tear down. Everything the
/// subscription owns lives in one composite (its service scope, the emission gate and the subject subscription), so a
/// disposed scope and a subject that has lost its observer together prove the composite was released rather than
/// dropped on the floor for the lifetime of the process.
/// </summary>
public class and_guard_denies_the_first_replayed_emission : given.a_guarded_sse_connection
{
    readonly ConcurrentQueue<SubscriptionScopeProbe> _probes = new();

    ReplaySubject<IEnumerable<string>> _replaySubject;

    void Establish()
    {
        _replaySubject = new ReplaySubject<IEnumerable<string>>(1);
        _replaySubject.OnNext(["buffered-before-anyone-subscribed"]);
        _streamingData = _replaySubject;
    }

    async Task Because() => await RunConnection(() => WaitFor(() => HasUnauthorizedFor(FirstQueryId)));

    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_return_401_from_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(401);
    [Fact] void should_dispose_the_subscription_scope() => _probes.Single().IsDisposed.ShouldBeTrue();
    [Fact] void should_stop_observing_the_subject() => _replaySubject.HasObservers.ShouldBeFalse();
    [Fact] void should_unregister_the_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);

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
