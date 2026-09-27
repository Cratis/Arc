// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_observing;

public class and_sorting_by_an_indexer_property : a_db_set_observe_context
{
    IEnumerable<Dictionary<string, object?>> _received;

    void Establish()
    {
        _dbContext.PropertyBags.AddRange(
            new Dictionary<string, object?> { ["Id"] = 1, ["Name"] = "zulu" },
            new Dictionary<string, object?> { ["Id"] = 2, ["Name"] = "alpha" });
        _dbContext.SaveChanges();
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), new Paging(0, 1, true), new Sorting("Name", SortDirection.Ascending));
    }

    void Because()
    {
        var subject = _dbContext.PropertyBags.Observe();
        _received = subject.FirstAsync().Timeout(TimeSpan.FromSeconds(10)).Wait();
        subject.OnCompleted();
    }

    [Fact] void should_sort_before_paging() => _received.Single()["Name"].ShouldEqual("alpha");
}
