// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_two_callers_dispose_during_interception : given.an_observable_query
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _firstCompletedBeforeRelease;
    bool _secondCompletedBeforeRelease;
    bool _sharedDisposal;
    bool _scopeReleasedBeforeInterceptionFinished;
    Exception? _exception;

    void Establish()
    {
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
        var interceptor = Substitute.For<IInterceptReadModel<ObservableScenarioReadModel>>();
        interceptor.Intercept(Arg.Any<ObservableScenarioReadModel>()).Returns(async call =>
        {
            var model = (ObservableScenarioReadModel)call[0];
            if (model.Name == "Blocked")
            {
                _entered.TrySetResult();
                await _release.Task;
                _scopeReleasedBeforeInterceptionFinished = _state.Scope.Disposed;
            }

            return model;
        });
        _scenario.Services.AddSingleton(interceptor);
    }

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
        using var cancellation = new CancellationTokenSource();
        _state.Subject.OnNext(new("Blocked"));
        var next = observation.Next(cancellationToken: cancellation.Token);
        try
        {
            await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await cancellation.CancelAsync();
            _exception = await Catch.Exception(() => next);
            await Task.Run(observation.Dispose).WaitAsync(TimeSpan.FromSeconds(5));
            var first = observation.DisposeAsync().AsTask();
            var second = _scenario.DisposeAsync().AsTask();
            _sharedDisposal = ReferenceEquals(first, observation.DisposeAsync().AsTask());
            _firstCompletedBeforeRelease = first.IsCompleted;
            _secondCompletedBeforeRelease = second.IsCompleted;
            _release.TrySetResult();
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _release.TrySetResult();
        }
    }

    [Fact] void should_cancel_the_read_without_abandoning_interception() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_share_one_observation_disposal_task() => _sharedDisposal.ShouldBeTrue();
    [Fact] void should_keep_the_first_disposal_pending() => _firstCompletedBeforeRelease.ShouldBeFalse();
    [Fact] void should_keep_scenario_disposal_pending() => _secondCompletedBeforeRelease.ShouldBeFalse();
    [Fact] void should_not_release_the_scope_during_interception() => _scopeReleasedBeforeInterceptionFinished.ShouldBeFalse();
    [Fact] void should_release_the_scope_once() => _state.Scope.DisposalCount.ShouldEqual(1);
    [Fact] void should_unsubscribe() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
}
