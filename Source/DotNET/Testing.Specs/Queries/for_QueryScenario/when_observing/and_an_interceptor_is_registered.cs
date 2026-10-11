// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_an_interceptor_is_registered : given.an_observable_query
{
    QueryObservation _observation = default!;
    QueryResult _result = default!;
    IInterceptReadModel<ObservableScenarioReadModel> _interceptor = default!;

    void Establish()
    {
        _interceptor = Substitute.For<IInterceptReadModel<ObservableScenarioReadModel>>();
        _interceptor.Intercept(Arg.Any<ObservableScenarioReadModel>())
            .Returns(call => new ObservableScenarioReadModel($"Intercepted {((ObservableScenarioReadModel)call[0]).Name}"));
        _scenario.Services.AddSingleton(_interceptor);
    }

    async Task Because()
    {
        _observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Collection));
        _state.Collection.OnNext([new("Changed")]);
        _result = await _observation.Next();
    }

    [Fact] void should_intercept_each_initial_item() => ((List<ObservableScenarioReadModel>)_observation.Initial.Data).Select(_ => _.Name).ShouldContainOnly("Intercepted First", "Intercepted Second");
    [Fact] void should_intercept_the_next_emission() => ((List<ObservableScenarioReadModel>)_result.Data).Single().Name.ShouldEqual("Intercepted Changed");
    [Fact] void should_intercept_every_item_in_both_emissions() => _interceptor.Received(3).Intercept(Arg.Any<ObservableScenarioReadModel>());
}
