// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_finite_async_enumerable_completes : given.a_guarded_sse_connection
{
    readonly CompletionState _state = new();
    bool _scopeDisposedAfterStreamExited;
    int _resultsAfterCompletion;

    void Establish() => _streamingData = Stream();

    async Task Because() => await RunConnection(async () =>
    {
        _state.Release.TrySetResult();
        await _state.StreamExited.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await _state.ScopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        _scopeDisposedAfterStreamExited = _state.ScopeDisposedAfterStreamExited;
        _resultsAfterCompletion = CountQueryResultsFor(FirstQueryId);
        await Task.Delay(50);
    });

    [Fact] void should_send_the_finite_result_once() => _resultsAfterCompletion.ShouldEqual(1);
    [Fact] void should_not_send_late_results() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_register_subscription_health() =>
        _healthTracker.Received(1).RegisterSubscription(Arg.Any<string>(), "SSE", Arg.Any<QuerySubscriptionMetadata>());
    [Fact] void should_unregister_subscription_health_on_completion() =>
        _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_dispose_the_guard_scope_after_the_stream_exits() => _scopeDisposedAfterStreamExited.ShouldBeTrue();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(_state);
        services.AddScoped<ScopedDependency>();
        guardTypes.Add(typeof(ScopedGuard));
    }

    async IAsyncEnumerable<IEnumerable<string>> Stream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await _state.Release.Task;
        try
        {
            yield return ["item-a"];
        }
        finally
        {
            _state.StreamExited.TrySetResult();
        }
    }

    public sealed class CompletionState
    {
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ScopeDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource StreamExited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ScopeDisposedAfterStreamExited { get; set; }
    }

    public class ScopedDependency(CompletionState state) : IDisposable
    {
        public void Dispose()
        {
            state.ScopeDisposedAfterStreamExited = state.StreamExited.Task.IsCompleted;
            state.ScopeDisposed.TrySetResult();
        }
    }

    public class ScopedGuard(ScopedDependency dependency) : IGuardObservableQueryEmission
    {
        public Task<ObservableQueryEmissionVerdict> Guard(ObservableQueryEmissionContext context)
        {
            GC.KeepAlive(dependency);
            return Task.FromResult(ObservableQueryEmissionVerdict.Allow);
        }
    }
}
