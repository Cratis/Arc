// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

/// <summary>
/// An async-enumerable guard can still be awaiting when its subscription is explicitly removed. Its eventual verdict
/// must observe the subscription cancellation before producing an authorization outcome or writing the emission.
/// </summary>
public class and_async_enumerable_guard_completes_after_unsubscribe : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource _guardRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _guardStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _streamEnded = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<ScopedDependency> _dependencies = [];
    bool _scopeWasAliveWhileGuardWasBlocked;

    void Establish() => _streamingData = Stream();

    async Task Because() => await RunConnection(async () =>
    {
        await _guardStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Unsubscribe(FirstQueryId);
        _scopeWasAliveWhileGuardWasBlocked = _dependencies.Count == 1 && !_dependencies.Single().IsDisposed;

        _guardRelease.TrySetResult();
        await _streamEnded.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await WaitFor(() => _dependencies.Single().IsDisposed);
    });

    [Fact] void should_have_reached_the_awaiting_guard() => _guardStarted.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_keep_the_subscription_scope_alive_while_the_guard_exits() => _scopeWasAliveWhileGuardWasBlocked.ShouldBeTrue();
    [Fact] void should_dispose_the_subscription_scope_after_the_guard_exits() => _dependencies.Single().IsDisposed.ShouldBeTrue();
    [Fact] void should_not_send_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_report_the_subscription_as_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_a_late_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_track_data_as_served() => _healthTracker.DidNotReceive().RecordDataServed(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_only_unregister_the_explicitly_unsubscribed_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(new AwaitingGuardState(_guardStarted, _guardRelease, _dependencies));
        services.AddScoped<ScopedDependency>();
        guardTypes.Add(typeof(AwaitingGuard));
    }

    async IAsyncEnumerable<IEnumerable<string>> Stream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            yield return ["item-a"];
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        finally
        {
            _streamEnded.TrySetResult();
        }
    }

    public sealed record AwaitingGuardState(
        TaskCompletionSource Started,
        TaskCompletionSource Release,
        List<ScopedDependency> Dependencies);

    public class ScopedDependency : IDisposable
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() => IsDisposed = true;
    }

    public class AwaitingGuard(AwaitingGuardState state, ScopedDependency dependency) : IGuardObservableQueryEmission
    {
        public async Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            state.Dependencies.Add(dependency);
            state.Started.TrySetResult();
            await state.Release.Task;
            return ObservableQueryEmissionVerdict.DenyAndTerminate;
        }
    }
}
