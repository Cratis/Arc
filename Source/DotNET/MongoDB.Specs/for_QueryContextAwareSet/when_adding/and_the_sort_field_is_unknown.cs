// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using SortDirection = Cratis.Arc.Queries.SortDirection;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_adding;

public class and_the_sort_field_is_unknown : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeClassWithSomeId _first;
    SomeClassWithSomeId _second;
    Exception _exception;

    void Establish()
    {
        _first = new(Guid.NewGuid(), 1);
        _second = new(Guid.NewGuid(), 2);
    }

    void Because() => _exception = Catch.Exception(() =>
    {
        _set = new(QueryContextBuilder.New()
            .WithSorting(new("ThisFieldDoesNotExist", SortDirection.Ascending))
            .Build());
        _set.Add(_first);
        _set.Add(_second);
    });

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_keep_all_items() => _set.Count().ShouldEqual(2);
}
