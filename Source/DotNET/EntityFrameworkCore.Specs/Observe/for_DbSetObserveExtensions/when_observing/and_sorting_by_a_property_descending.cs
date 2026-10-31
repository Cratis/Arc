// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

public class and_sorting_by_a_property_descending : a_db_set_observe_context
{
    IEnumerable<TestEntity> _received;

    void Establish()
    {
        SeedTestData(
            new TestEntity { Name = "low", SortOrder = 1 },
            new TestEntity { Name = "high", SortOrder = 3 },
            new TestEntity { Name = "middle", SortOrder = 2 });
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), new Paging(0, 2, true), new Sorting("SortOrder", SortDirection.Descending));
    }

    void Because()
    {
        var subject = _dbContext.TestEntities.Observe();
        _received = subject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        subject.OnCompleted();
    }

    [Fact] void should_sort_descending_before_paging() => _received.Select(_ => _.Name).ShouldEqual(["high", "middle"]);
}
