// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_subject_completes : given.a_guarded_sse_connection
{
    readonly CompletionState _state = new();
    bool _hadObserversAfterCompletion;
    int _resultsAfterCompletion;

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["item-a"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);

        _subject.OnCompleted();
        await _state.ScopeDisposed.Task.WaitAsync(TimeSpan.FromSeconds(2));
        _hadObserversAfterCompletion = _subject.HasObservers;
        _resultsAfterCompletion = CountQueryResultsFor(FirstQueryId);

        _subject.OnNext(["late-item"]);
        await Task.Delay(50);
    });

    [Fact] void should_send_the_last_emission_once() => _resultsAfterCompletion.ShouldEqual(1);
    [Fact] void should_detach_the_subject_observer() => _hadObserversAfterCompletion.ShouldBeFalse();
    [Fact] void should_dispose_the_subscription_scope() => _state.ScopeDisposed.Task.IsCompleted.ShouldBeTrue();
    [Fact] void should_unregister_subscription_health() =>
        _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_not_send_a_late_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_not_send_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();

    protected override void ConfigureGuards(IServiceCollection services, List<Type> guardTypes)
    {
        services.AddSingleton(_state);
        services.AddScoped<ScopedDependency>();
        guardTypes.Add(typeof(ScopedGuard));
    }

    public sealed class CompletionState
    {
        public TaskCompletionSource ScopeDisposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public class ScopedDependency(CompletionState state) : IDisposable
    {
        public void Dispose() => state.ScopeDisposed.TrySetResult();
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
