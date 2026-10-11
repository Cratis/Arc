// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario.when_performing_an_observable_query;

public class and_it_emits_a_collection : given.an_observable_query
{
    QueryResult _result = default!;

    async Task Because() => _result = await _scenario.Perform(nameof(ObservableScenarioReadModel.Collection), paging: new Paging(2, 10, true));

    [Fact] void should_materialize_a_list() => _result.Data.ShouldBeOfExactType<List<ObservableScenarioReadModel>>();
    [Fact] void should_include_all_items() => ((List<ObservableScenarioReadModel>)_result.Data).Select(_ => _.Name).ShouldContainOnly("First", "Second");
    [Fact] void should_capture_the_total() => _result.Paging.TotalItems.ShouldEqual(42);
    [Fact] void should_capture_the_page() => _result.Paging.Page.ShouldEqual(2);
}
