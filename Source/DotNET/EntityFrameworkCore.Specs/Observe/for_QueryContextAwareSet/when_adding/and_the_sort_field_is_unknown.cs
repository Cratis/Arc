// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_QueryContextAwareSet.when_adding;

public class and_the_sort_field_is_unknown : Specification
{
    QueryContextAwareSet<SomeEntity> _set;
    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
    {
        var queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, new Sorting("ThisFieldDoesNotExist", SortDirection.Ascending));
        _set = new QueryContextAwareSet<SomeEntity>(queryContext);
        _set.Add(new(Guid.NewGuid(), 1));
        _set.Add(new(Guid.NewGuid(), 2));
    });

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_keep_all_items() => _set.Count().ShouldEqual(2);

    record SomeEntity(Guid Id, int Value);
}
