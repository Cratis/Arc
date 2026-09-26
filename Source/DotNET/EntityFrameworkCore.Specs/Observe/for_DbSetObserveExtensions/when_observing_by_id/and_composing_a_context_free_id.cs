// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing_by_id;

public class and_composing_a_context_free_id : a_composed_observation
{
    AuxiliaryEntity _observed;
    int _initialTotal;
    int _updatedTotal;

    void Because()
    {
        var primary = _dbContext.TestEntities.Observe();
        var auxiliary = _dbContext.AuxiliaryEntities.ObserveById(_target.Id, ignoreQueryContext: true);
        _observed = auxiliary.FirstAsync().Wait();
        _initialTotal = _queryContext.TotalItems;

        InsertAuxiliary("Unrelated", false);
        _updatedTotal = _queryContext.TotalItems;

        auxiliary.OnCompleted();
        primary.OnCompleted();
    }

    [Fact] void should_find_the_id_despite_the_requested_second_page() => _observed.Id.ShouldEqual(_target.Id);
    [Fact] void should_leave_the_primary_total_intact_after_startup() => _initialTotal.ShouldEqual(4);
    [Fact] void should_leave_the_primary_total_intact_after_a_change() => _updatedTotal.ShouldEqual(4);
    [Fact] void should_never_count_the_auxiliary_source() => _auxiliaryCountInterceptor.CountQueries.ShouldEqual(0);
}
