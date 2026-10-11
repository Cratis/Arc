// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_no_change_happens : given.an_observable_query
{
    Exception? _exception;
    QueryResult _afterTimeout = default!;

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Current));
        _exception = await Catch.Exception(() => observation.Next(TimeSpan.FromMilliseconds(50)));
        _state.Subject.OnNext(new("After timeout"));
        _afterTimeout = await observation.Next();
    }

    [Fact] void should_time_out() => _exception.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_keep_the_observation_live() => ((ObservableScenarioReadModel)_afterTimeout.Data).Name.ShouldEqual("After timeout");
}
