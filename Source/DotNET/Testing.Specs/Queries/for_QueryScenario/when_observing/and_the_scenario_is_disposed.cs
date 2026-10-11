// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_scenario_is_disposed : given.an_observable_query
{
    bool _scopeWasLive;
    Exception? _exception;

    void Establish()
    {
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
    }

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
        _scopeWasLive = !_state.Scope.Disposed;
        await _scenario.DisposeAsync();
        await _scenario.DisposeAsync();
        _exception = await Catch.Exception(() => observation.Next());
    }

    [Fact] void should_keep_the_owned_scope_alive_during_observation() => _scopeWasLive.ShouldBeTrue();
    [Fact] void should_unsubscribe() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
    [Fact] void should_dispose_the_owned_scope() => _state.Scope.Disposed.ShouldBeTrue();
    [Fact] void should_unsubscribe_before_disposing_the_scope() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
    [Fact] void should_end_the_observation() => _exception.ShouldBeOfExactType<ObjectDisposedException>();
}
