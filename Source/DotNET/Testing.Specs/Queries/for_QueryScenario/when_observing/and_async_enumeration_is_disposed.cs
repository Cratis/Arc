// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_async_enumeration_is_disposed : given.an_observable_query
{
    QueryResult _result = default!;

    void Establish() => _state.Changes.Values.Writer.TryWrite(new("Initial"));

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Async));
        var next = observation.Next();
        _state.Changes.Values.Writer.TryWrite(new("Changed"));
        _result = await next;
        await observation.DisposeAsync();
    }

    [Fact] void should_receive_the_changed_item() => ((ObservableScenarioReadModel)_result.Data).Name.ShouldEqual("Changed");
    [Fact] void should_wait_for_enumeration_teardown() => _state.Changes.EnumerationStopped.ShouldBeTrue();
}
