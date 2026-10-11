// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_subscription_teardown_disposes_the_observation : given.an_observable_query
{
    readonly DisposingObservationSubject _subject = new();
    QueryObservation _observation = default!;

    async Task Establish()
    {
        _state.Subject = _subject;
        _scenario.Services.AddSingleton<IAuthorizationPolicyRuntime>(new ChangingPrincipalRuntime());
        _scenario.Services.AddScoped<ObservableQueryScope>();
        _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Scoped));
        _subject.DisposeObservation = _observation.Dispose;
    }

    async Task Because()
    {
        await Task.Run(async () => await _scenario.DisposeAsync()).WaitAsync(TimeSpan.FromSeconds(5));
        await _observation.DisposeAsync();
    }

    [Fact] void should_return_from_subscription_teardown() => _subject.TeardownReturned.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_dispose_the_subscription_once() => _subject.SubscriptionDisposals.ShouldEqual(1);
    [Fact] void should_unsubscribe() => _subject.HasObservers.ShouldBeFalse();
    [Fact] void should_release_the_scope_once() => _state.Scope.DisposalCount.ShouldEqual(1);
    [Fact] void should_unsubscribe_before_releasing_the_scope() => _state.Scope.HadObserversAtDisposal.ShouldBeFalse();
}
