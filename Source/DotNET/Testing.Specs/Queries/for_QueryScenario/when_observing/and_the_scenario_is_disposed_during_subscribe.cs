// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_scenario_is_disposed_during_subscribe : given.an_observable_query
{
    readonly StartupBlockingSubject _subject = new();
    bool _disposalCompletedBeforeStartup;
    bool _scopeWasLiveDuringStartup;
    Exception? _exception;

    void Establish()
    {
        _state.Subject = _subject;
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
    }

    async Task Because()
    {
        var observe = Task.Run(() => _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped)));
        try
        {
            await _subject.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var disposal = _scenario.DisposeAsync().AsTask();
            _disposalCompletedBeforeStartup = disposal.IsCompleted;
            _scopeWasLiveDuringStartup = !_state.Scope.Disposed;
            _subject.Release.TrySetResult();
            await disposal.WaitAsync(TimeSpan.FromSeconds(5));
            _exception = await Catch.Exception(() => observe);
        }
        finally
        {
            _subject.Release.TrySetResult();
        }
    }

    [Fact] void should_not_complete_disposal_before_startup_settles() => _disposalCompletedBeforeStartup.ShouldBeFalse();
    [Fact] void should_keep_the_scope_alive_while_subscribing() => _scopeWasLiveDuringStartup.ShouldBeTrue();
    [Fact] void should_dispose_the_late_subscription_once() => _subject.SubscriptionDisposals.ShouldEqual(1);
    [Fact] void should_release_the_scope_once() => _state.Scope.DisposalCount.ShouldEqual(1);
    [Fact] void should_release_the_scope_after_unsubscribing() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
    [Fact] void should_end_the_pending_observe() => (_exception is ObjectDisposedException or OperationCanceledException).ShouldBeTrue();
}
