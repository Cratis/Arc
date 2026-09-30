// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions;

public class when_paging_an_ordered_queryable : Specification
{
    record item(string Name);

    IQueryable _queryable;
    item[] _actualCollection = [new("d"), new("b"), new("a"), new("c")];
    IQueryable _paged;
    item[] _result;

    void Establish() => _queryable = _actualCollection.AsQueryable();

    void Because()
    {
        _paged = _queryable.OrderBy(nameof(item.Name)).Skip(1).Take(2);
        _result = [.. _paged.Cast<item>()];
    }

    [Fact] void should_keep_the_element_type() => _paged.ElementType.ShouldEqual(typeof(item));
    [Fact] void should_return_the_ordered_page() => _result.ShouldEqual(_actualCollection.OrderBy(_ => _.Name).Skip(1).Take(2));
}
