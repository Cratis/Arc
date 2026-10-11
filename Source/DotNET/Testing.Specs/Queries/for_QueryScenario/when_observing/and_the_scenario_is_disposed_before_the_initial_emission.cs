// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_scenario_is_disposed_before_the_initial_emission : given.a_blocked_emission_guard
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
        var observe = _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await _scenario.DisposeAsync();
        _exception = await Catch.Exception(() => observe);
    }

    [Fact] void should_end_the_initial_wait() => (_exception is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_unsubscribe_the_pending_observation() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
    [Fact] void should_release_the_owned_scope() => _state.Scope.Disposed.ShouldBeTrue();
    [Fact] void should_unsubscribe_before_releasing_the_scope() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
}
