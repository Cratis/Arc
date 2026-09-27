// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

public class and_composing_a_context_free_collection : a_composed_observation
{
    IEnumerable<TestEntity> _primaryPage;
    IEnumerable<AuxiliaryEntity> _initialAuxiliary;
    IEnumerable<AuxiliaryEntity> _updatedAuxiliary;
    int _auxiliaryOnlyTotal;
    int _initialTotal;
    int _updatedTotal;

    void Because()
    {
        var auxiliary = _dbContext.AuxiliaryEntities.Observe(entity => entity.IsActive, ignoreQueryContext: true);
        _initialAuxiliary = auxiliary.FirstAsync().Wait().ToList();
        _auxiliaryOnlyTotal = _queryContext.TotalItems;
        var primary = _dbContext.TestEntities.Observe();
        _primaryPage = primary.FirstAsync().Wait().ToList();
        _initialTotal = _queryContext.TotalItems;

        InsertAuxiliary("Auxiliary D", true);
        _updatedAuxiliary = auxiliary.FirstAsync().Wait().ToList();
        _updatedTotal = _queryContext.TotalItems;

        auxiliary.OnCompleted();
        primary.OnCompleted();
    }

    [Fact] void should_preserve_paging_and_sorting_on_the_primary() => _primaryPage.Select(entity => entity.Name).ShouldContainOnly(["Primary C"]);
    [Fact] void should_return_every_matching_auxiliary_entity() => _initialAuxiliary.Select(entity => entity.Name).ShouldContainOnly(["Auxiliary A", "Auxiliary B"]);
    [Fact] void should_not_page_or_sort_the_auxiliary_after_a_change() => _updatedAuxiliary.Select(entity => entity.Name).ShouldContainOnly(["Auxiliary A", "Auxiliary B", "Auxiliary D"]);
    [Fact] void should_not_write_the_total_before_the_primary_starts() => _auxiliaryOnlyTotal.ShouldEqual(0);
    [Fact] void should_leave_the_primary_total_intact_after_startup() => _initialTotal.ShouldEqual(4);
    [Fact] void should_leave_the_primary_total_intact_after_a_change() => _updatedTotal.ShouldEqual(4);
    [Fact] void should_never_count_the_auxiliary_source() => _auxiliaryCountInterceptor.CountQueries.ShouldEqual(0);
    [Fact] void should_not_mutate_the_shared_query_context() => _queryContext.Paging.IsPaged.ShouldBeTrue();
}
