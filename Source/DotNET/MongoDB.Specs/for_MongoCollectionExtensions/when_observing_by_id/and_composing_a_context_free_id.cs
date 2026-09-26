// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_by_id;

public class and_composing_a_context_free_id : given.a_composed_observation
{
    ObservedDocument _target;
    ObservedDocument? _first;
    int _total;

    async Task Because()
    {
        _target = _auxiliaryDocuments[0];
        _auxiliaryDocuments = [_target];
        var primary = _collection.Observe();
        var secondary = _auxiliary.ObserveById(_target.Id, ignoreQueryContext: true);
        var primaryEmission = FirstEmission(primary, TimeSpan.FromSeconds(5));
        var secondaryEmission = FirstSingleEmission(secondary, TimeSpan.FromSeconds(5));
        ReleaseInitialQueries();
        await Task.WhenAll(primaryEmission, secondaryEmission);
        _first = await secondaryEmission;
        _total = _queryContext.TotalItems;
    }

    [Fact] void should_find_the_id_despite_the_requested_second_page() => _first.ShouldEqual(_target);
    [Fact] void should_not_page_the_id_observation() => _auxiliaryFindOptions.Skip.ShouldBeNull();
    [Fact] void should_not_sort_the_id_observation() => _auxiliaryFindOptions.Sort.ShouldBeNull();
    [Fact] void should_not_replace_the_primary_total() => _total.ShouldEqual(_documents.Count);
    [Fact] async Task should_not_count_the_auxiliary_source() => await _auxiliary.DidNotReceive().CountDocumentsAsync(
        Arg.Any<FilterDefinition<ObservedDocument>>(), Arg.Any<CountOptions>(), Arg.Any<CancellationToken>());
}
