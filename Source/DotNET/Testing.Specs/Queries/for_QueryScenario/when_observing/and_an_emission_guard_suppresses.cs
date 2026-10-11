// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_an_emission_guard_suppresses : given.an_observable_query
{
    QueryResult _result = default!;
    IObservableQueryEmissionGuards _guards = default!;

    void Establish()
    {
        _guards = Substitute.For<IObservableQueryEmissionGuards>();
        _guards.HasGuards.Returns(true);
        _guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(ObservableQueryEmissionVerdict.Allow, ObservableQueryEmissionVerdict.Suppress, ObservableQueryEmissionVerdict.Allow);
        _scenario.Services.AddSingleton(_guards);
    }

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        _state.Subject.OnNext(new("Suppressed"));
        _state.Subject.OnNext(new("Allowed"));
        _result = await observation.Next();
    }

    [Fact] void should_skip_the_suppressed_value() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("Allowed");
    [Fact] void should_evaluate_the_initial_emission_as_first() => _guards.Received(1).Guard(Arg.Is<ObservableQueryEmissionContext>(_ => _.IsFirstEmission));
    [Fact] void should_evaluate_both_subsequent_emissions() => _guards.Received(2).Guard(Arg.Is<ObservableQueryEmissionContext>(_ => !_.IsFirstEmission));
}
