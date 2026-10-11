// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_observation_is_disposed : given.an_observable_query
{
    QueryObservation _observation = default!;
    bool _scopeWasLive;

    async Task Establish()
    {
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
        _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
        _scopeWasLive = !_state.Scope.Disposed;
    }

    void Because()
    {
        _observation.Dispose();
        _observation.Dispose();
    }

    [Fact] void should_keep_the_owned_scope_alive_until_disposal() => _scopeWasLive.ShouldBeTrue();
    [Fact] void should_unsubscribe() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
    [Fact] void should_dispose_the_owned_scope() => _state.Scope.Disposed.ShouldBeTrue();
    [Fact] void should_unsubscribe_before_disposing_the_scope() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
    [Fact] async Task should_reject_subsequent_reads() => (await Catch.Exception(() => _observation.Next())).ShouldBeOfExactType<ObjectDisposedException>();
    [Fact] async Task should_allow_repeated_async_disposal() => await _observation.DisposeAsync();
    [Fact] void should_not_dispose_the_subject() => _state.Subject.OnNext(new("Still usable"));
}
