// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_the_query_returns_a_snapshot : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    QueryObservation _observation = default!;
    Exception? _exception;

    async Task Because()
    {
        _observation = await _scenario.Observe(nameof(ScenarioReadModel.All));
        _exception = await Catch.Exception(() => _observation.Next(TimeSpan.FromMilliseconds(50)));
    }

    [Fact] void should_return_the_snapshot_as_initial() => ((ScenarioReadModel)_observation.Initial.Data).Name.ShouldEqual("All");
    [Fact] void should_succeed_initially() => _observation.Initial.IsSuccess.ShouldBeTrue();
    [Fact] void should_wait_for_an_emission_instead_of_disposing_the_observation() => _exception.ShouldBeOfExactType<TimeoutException>();

    async Task Destroy() => await _scenario.DisposeAsync();
}
