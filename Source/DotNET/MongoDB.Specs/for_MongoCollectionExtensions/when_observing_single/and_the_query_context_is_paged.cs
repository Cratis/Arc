// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

public class and_the_query_context_is_paged : given.an_observed_collection
{
    void Establish()
    {
        _documents = [_documents[0]];
        _queryContext = _queryContext with
        {
            Paging = new Paging(2, 1, true),
            Sorting = new Sorting(nameof(ObservedDocument.Name), Cratis.Arc.Queries.SortDirection.Descending),
            TotalItems = 73
        };
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_ignore_paging_sorting_and_counting(string overload)
    {
        var target = _documents[0];
        var filter = Builders<ObservedDocument>.Filter.Eq(document => document.Id, target.Id);
        var subject = overload switch
        {
            "expression" => _collection.ObserveSingle(document => document.Id == target.Id),
            "filter" => _collection.ObserveSingle(filter),
            "by_id" => _collection.ObserveById(target.Id),
            "expression_false" => _collection.ObserveSingle(document => document.Id == target.Id, ignoreQueryContext: false),
            "filter_false" => _collection.ObserveSingle(filter, ignoreQueryContext: false),
            _ => _collection.ObserveById(target.Id, ignoreQueryContext: false)
        };
        using var emissions = RecordEmissions(subject);
        _initialQueryGate.SetResult();
        var initial = await emissions.Next(TimeSpan.FromSeconds(5));
        initial.ShouldEqual(target);
        _primaryFindOptions.Skip.ShouldBeNull();
        _primaryFindOptions.Limit.ShouldBeNull();
        _primaryFindOptions.Sort.ShouldBeNull();
        _queryContext.TotalItems.ShouldEqual(73);

        PushChange(DeleteOf(target.Id));
        var removed = await emissions.Next(TimeSpan.FromSeconds(5));
        removed.ShouldBeNull();
        _queryContext.TotalItems.ShouldEqual(73);
        await _collection.DidNotReceive().CountDocumentsAsync(
            Arg.Any<FilterDefinition<ObservedDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>());
    }
}
