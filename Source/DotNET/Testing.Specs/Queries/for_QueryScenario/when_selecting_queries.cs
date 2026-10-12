// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;
using Cratis.Arc.Testing.Queries;

namespace Cratis.Arc.Testing.for_QueryScenario;

public class when_selecting_queries : Specification
{
    readonly QueryScenario<NoQueries> _empty = new();
    readonly QueryScenario<SelectedReadModel> _query = new();
    QueryResult _emptyResult;
    QueryResult _hiddenResult;
    QueryResult _queryResult;
    QueryObservation _observation;
    readonly List<QueryResult> _hookResults = [];

    async Task Because()
    {
        _emptyResult = await _empty.Perform("Create");
        _hiddenResult = await _query.Perform("Create");
        _queryResult = await _query.Perform("ById", new QueryArguments { ["id"] = "query" });
        _observation = await _query.Observe("Observe");
        foreach (var hook in new[] { "BeginReplay", "EndReplay", "BeginReplayPartition", "EndReplayPartition" })
        {
            _hookResults.Add(await _query.Perform(hook));
        }
    }

    [Fact] void should_allow_zero_queries() => _emptyResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_perform_an_unmarked_helper() => _hiddenResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_perform_unmarked_task_lifecycle_hooks() => _hookResults.TrueForAll(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_perform_the_marked_overload() => ((SelectedReadModel)_queryResult.Data).Id.ShouldEqual("query");
    [Fact] void should_observe_a_marked_subject_collection() => ((IEnumerable<SelectedReadModel>)_observation.Initial.Data).Single().Id.ShouldEqual("initial");

    async Task Destroy()
    {
        await _observation.DisposeAsync();
        await _empty.DisposeAsync();
        await _query.DisposeAsync();
    }
}
