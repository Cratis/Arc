// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Arc.Queries;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_single;

public class and_the_query_context_is_paged : given.an_observed_collection
{
    static readonly Dictionary<SingleObservation, Func<IMongoCollection<ObservedDocument>, ObservedDocument, ISubject<ObservedDocument>>> _observations = new()
    {
        [SingleObservation.Expression] = (collection, target) => collection.ObserveSingle(document => document.Id == target.Id),
        [SingleObservation.Filter] = (collection, target) => collection.ObserveSingle(Builders<ObservedDocument>.Filter.Eq(document => document.Id, target.Id)),
        [SingleObservation.ById] = (collection, target) => collection.ObserveById(target.Id),
        [SingleObservation.ExpressionWithContext] = (collection, target) => collection.ObserveSingle(document => document.Id == target.Id, ignoreQueryContext: false),
        [SingleObservation.FilterWithContext] = (collection, target) => collection.ObserveSingle(Builders<ObservedDocument>.Filter.Eq(document => document.Id, target.Id), ignoreQueryContext: false),
        [SingleObservation.ByIdWithContext] = (collection, target) => collection.ObserveById(target.Id, ignoreQueryContext: false)
    };

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
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_deliver_the_document_ignoring_paging(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        _initial.ShouldEqual(_documents[0]);
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_not_skip(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        _primaryFindOptions.Skip.ShouldBeNull();
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_not_limit(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        _primaryFindOptions.Limit.ShouldBeNull();
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_not_sort(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        _primaryFindOptions.Sort.ShouldBeNull();
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_not_change_total_items_on_the_initial_query(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        _queryContext.TotalItems.ShouldEqual(73);
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_report_removal_as_absent(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        (await RemoveObserved(emissions)).ShouldBeNull();
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_not_change_total_items_on_removal(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        await RemoveObserved(emissions);
        _queryContext.TotalItems.ShouldEqual(73);
    }

    [Theory]
    [InlineData(SingleObservation.Expression)]
    [InlineData(SingleObservation.Filter)]
    [InlineData(SingleObservation.ById)]
    [InlineData(SingleObservation.ExpressionWithContext)]
    [InlineData(SingleObservation.FilterWithContext)]
    [InlineData(SingleObservation.ByIdWithContext)]
    public async Task should_not_count_documents(SingleObservation overload)
    {
        using var emissions = await ObserveInitial(overload);
        await RemoveObserved(emissions);
        await _collection.DidNotReceive().CountDocumentsAsync(
            Arg.Any<FilterDefinition<ObservedDocument>>(),
            Arg.Any<CountOptions>(),
            Arg.Any<CancellationToken>());
    }

    async Task<EmissionSequence> ObserveInitial(SingleObservation overload)
    {
        var emissions = RecordEmissions(Observe(overload, _documents[0]));
        _initialQueryGate.SetResult();
        _initial = await emissions.Next(TimeSpan.FromSeconds(5));
        return emissions;
    }

    ISubject<ObservedDocument> Observe(SingleObservation overload, ObservedDocument target) => _observations[overload](_collection, target);

    async Task<ObservedDocument?> RemoveObserved(EmissionSequence emissions)
    {
        PushChange(DeleteOf(_documents[0].Id));
        return await emissions.Next(TimeSpan.FromSeconds(5));
    }
}
