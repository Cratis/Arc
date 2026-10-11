// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_query_is_unauthorized : given.an_observable_query
{
    QueryObservation _observation = default!;

    void Establish()
    {
        var accessor = Substitute.For<ICurrentPrincipalAccessor>();
        accessor.Current.Returns(new ClaimsPrincipal(new ClaimsIdentity([], "test")));
        _scenario.Services.AddSingleton(accessor);
        _scenario.Services.AddArcAuthorizationPolicy<CanReadPolicy>("CanRead");
    }

    async Task Because() => _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Restricted));

    [Fact] void should_reject_the_initial_result() => _observation.Initial.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_subscribe() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
}
