// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_a_guard_cancellation_callback_disposes_the_observation : given.an_observable_query
{
    readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _callbackReturned = new(TaskCreationOptions.RunContinuationsAsynchronously);
    QueryObservation _observation = default!;
    int _cancellationCallbacks;
    Exception? _exception;

    async Task Establish()
    {
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(async call =>
        {
            var context = (ObservableQueryEmissionContext)call[0];
            if (!context.IsFirstEmission)
            {
                await using var registration = context.CancellationToken.Register(() =>
                {
                    _cancellationCallbacks++;
                    _observation.Dispose();
                    _callbackReturned.TrySetResult();
                });
                _entered.TrySetResult();
                await _callbackReturned.Task.WaitAsync(TimeSpan.FromSeconds(5));
                context.CancellationToken.ThrowIfCancellationRequested();
            }

            return ObservableQueryEmissionVerdict.Allow;
        });
        _scenario.Services.AddSingleton(guards);
        _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
    }

    async Task Because()
    {
        _state.Subject.OnNext(new("Blocked"));
        var next = _observation.Next();
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Run(async () => await _scenario.DisposeAsync()).WaitAsync(TimeSpan.FromSeconds(5));
        await _observation.DisposeAsync();
        _exception = await Catch.Exception(() => next);
    }

    [Fact] void should_return_from_the_cancellation_callback() => _callbackReturned.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_run_the_cancellation_callback_once() => _cancellationCallbacks.ShouldEqual(1);
    [Fact] void should_end_the_pending_read() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_unsubscribe() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
    [Fact] void should_release_the_scope_once() => _state.Scope.DisposalCount.ShouldEqual(1);
    [Fact] void should_unsubscribe_before_releasing_the_scope() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
}
