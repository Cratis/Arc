// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_an_emission_guard_denies : given.an_observable_query
{
    QueryResult _result = default!;

    void Establish()
    {
        var guards = Substitute.For<IObservableQueryEmissionGuards>();
        guards.HasGuards.Returns(true);
        guards.Guard(Arg.Any<ObservableQueryEmissionContext>()).Returns(ObservableQueryEmissionVerdict.Allow, ObservableQueryEmissionVerdict.DenyAndTerminate);
        _scenario.Services.AddSingleton(guards);
    }

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        _state.Subject.OnNext(new("Denied"));
        _result = await observation.Next();
    }

    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_leak_data() => _result.Data.ShouldBeNull();
    [Fact] void should_end_the_subscription() => ((BehaviorSubject<ObservableScenarioReadModel>)_state.Subject).HasObservers.ShouldBeFalse();
}
