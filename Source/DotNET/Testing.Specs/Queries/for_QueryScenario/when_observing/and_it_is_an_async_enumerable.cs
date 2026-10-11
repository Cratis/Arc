// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_it_is_an_async_enumerable : Specification
{
    readonly QueryScenario<ScenarioReadModel> _scenario = new();
    QueryObservation _observation = default!;
    QueryResult _result = default!;

    async Task Because()
    {
        _observation = await _scenario.Observe(nameof(ScenarioReadModel.AsyncStream));
        _result = await _observation.Next();
    }

    [Fact] void should_receive_the_first_item() => ((ScenarioReadModel)_observation.Initial.Data).Name.ShouldEqual("First");
    [Fact] void should_receive_the_next_item() => ((ScenarioReadModel)_result.Data).Name.ShouldEqual("Second");

    async Task Destroy() => await _scenario.DisposeAsync();
}
