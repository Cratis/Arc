// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_initial_wait_is_canceled_during_a_guard : given.a_blocked_emission_guard
{
    Exception? _exception;

    void Establish()
    {
        _blockInitial = true;
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
    }

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        var observe = _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped), cancellationToken: cancellation.Token);
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancellation.CancelAsync();
        _exception = await Catch.Exception(() => observe);
        await _scenario.DisposeAsync();
    }

    [Fact] void should_report_cancellation() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_unsubscribe_on_failure() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
    [Fact] void should_release_the_owned_scope() => _state.Scope.Disposed.ShouldBeTrue();
    [Fact] void should_unsubscribe_before_releasing_the_scope() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
}
