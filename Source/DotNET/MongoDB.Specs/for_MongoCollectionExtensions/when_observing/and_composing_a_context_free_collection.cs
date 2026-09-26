// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing;

public class and_composing_a_context_free_collection : given.a_composed_observation
{
    ISubject<IEnumerable<ObservedDocument>> _primary;
    ISubject<IEnumerable<ObservedDocument>> _secondary;
    IEnumerable<ObservedDocument> _initialSecondary;
    IEnumerable<ObservedDocument> _updatedSecondary;
    int _initialTotal;
    int _updatedTotal;

    async Task Because()
    {
        _primary = _collection.Observe();
        _secondary = _auxiliary.Observe(ignoreQueryContext: true);
        var primaryEmission = FirstEmission(_primary, TimeSpan.FromSeconds(5));
        var initial = new TaskCompletionSource<IEnumerable<ObservedDocument>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var updated = new TaskCompletionSource<IEnumerable<ObservedDocument>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var emissionCount = 0;
        using var subscription = _secondary.Subscribe(documents =>
        {
            if (Interlocked.Increment(ref emissionCount) == 1)
            {
                initial.TrySetResult(documents);
            }
            else
            {
                updated.TrySetResult(documents);
            }
        });
        ReleaseInitialQueries();
        await Task.WhenAll(primaryEmission, initial.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        _initialSecondary = await initial.Task;
        _initialTotal = _queryContext.TotalItems;

        PushAuxiliaryChange(InsertOf(new(Guid.NewGuid(), "Auxiliary D")));
        _updatedSecondary = await updated.Task.WaitAsync(TimeSpan.FromSeconds(5));
        _updatedTotal = _queryContext.TotalItems;
    }

    [Fact] void should_retain_primary_paging() => _primaryFindOptions.Skip.ShouldEqual(1);
    [Fact] void should_retain_primary_limit() => _primaryFindOptions.Limit.ShouldEqual(1);
    [Fact] void should_retain_primary_sort() => _primaryFindOptions.Sort.ShouldNotBeNull();
    [Fact] void should_not_page_the_auxiliary_source() => _auxiliaryFindOptions.Skip.ShouldBeNull();
    [Fact] void should_not_limit_the_auxiliary_source() => _auxiliaryFindOptions.Limit.ShouldBeNull();
    [Fact] void should_not_sort_the_auxiliary_source() => _auxiliaryFindOptions.Sort.ShouldBeNull();
    [Fact] void should_emit_the_full_auxiliary_source() => _initialSecondary.ShouldContainOnly(_auxiliaryDocuments);
    [Fact] void should_retain_the_primary_total_after_concurrent_first_emissions() => _initialTotal.ShouldEqual(_documents.Count);
    [Fact] void should_keep_all_auxiliary_items_after_an_insert() => _updatedSecondary.Count().ShouldEqual(_auxiliaryDocuments.Count + 1);
    [Fact] void should_not_change_the_primary_total_on_auxiliary_updates() => _updatedTotal.ShouldEqual(_documents.Count);
}
