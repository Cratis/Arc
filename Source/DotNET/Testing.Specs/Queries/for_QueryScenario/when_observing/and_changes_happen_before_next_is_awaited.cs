// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_observing;

public class and_changes_happen_before_next_is_awaited : given.an_observable_query
{
    QueryResult _first = default!;
    QueryResult _second = default!;

    async Task Because()
    {
        var observation = await _scenario.Observe(nameof(ObservableScenarioReadModel.Collection), paging: new Paging(0, 10, true));
        _state.Context.TotalItems = 5;
        var items = new List<ObservableScenarioReadModel> { new("First change") };
        _state.Collection.OnNext(items);
        items.Clear();
        _state.Context.TotalItems = 6;
        _state.Collection.OnNext([new("Second change")]);
        _state.Context.TotalItems = 99;
        _first = await observation.Next();
        _second = await observation.Next();
    }

    [Fact] void should_buffer_the_first_change() => ((List<ObservableScenarioReadModel>)_first.Data).Single().Name.ShouldEqual("First change");
    [Fact] void should_buffer_the_second_change_in_order() => ((List<ObservableScenarioReadModel>)_second.Data).Single().Name.ShouldEqual("Second change");
    [Fact] void should_capture_the_first_total_at_emission() => _first.Paging.TotalItems.ShouldEqual(5);
    [Fact] void should_capture_the_second_total_at_emission() => _second.Paging.TotalItems.ShouldEqual(6);
}
