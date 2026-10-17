// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

public class and_the_query_context_is_paged : given.an_observed_collection
{
    ObservedDocument? _initial;

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
    public async Task should_deliver_the_document_ignoring_paging(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        _initial.ShouldEqual(_documents[0]);
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_not_skip(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        _primaryFindOptions.Skip.ShouldBeNull();
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_not_limit(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        _primaryFindOptions.Limit.ShouldBeNull();
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_not_sort(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        _primaryFindOptions.Sort.ShouldBeNull();
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_not_change_total_items_on_the_initial_query(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        _queryContext.TotalItems.ShouldEqual(73);
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_report_removal_as_absent(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        (await RemoveObserved(emissions)).ShouldBeNull();
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_not_change_total_items_on_removal(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        await RemoveObserved(emissions);
        _queryContext.TotalItems.ShouldEqual(73);
    }

    [Theory]
    [InlineData("expression")]
    [InlineData("filter")]
    [InlineData("by_id")]
    [InlineData("expression_false")]
    [InlineData("filter_false")]
    [InlineData("by_id_false")]
    public async Task should_not_count_documents(string overload)
    {
        using var emissions = await ObserveInitial(overload);
        await RemoveObserved(emissions);
        await _collection.DidNotReceive().CountDocumentsAsync(
            Arg.Any<FilterDefinition<ObservedDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>());
    }

    async Task<EmissionSequence> ObserveInitial(string overload)
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
        var emissions = RecordEmissions(subject);
        _initialQueryGate.SetResult();
        _initial = await emissions.Next(TimeSpan.FromSeconds(5));
        return emissions;
    }

    async Task<ObservedDocument?> RemoveObserved(EmissionSequence emissions)
    {
        PushChange(DeleteOf(_documents[0].Id));
        return await emissions.Next(TimeSpan.FromSeconds(5));
    }
}
