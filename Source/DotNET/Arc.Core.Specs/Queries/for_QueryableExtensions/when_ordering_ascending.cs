// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions;

public class when_ordering_ascending : Specification
{
    record item(int Value);

    IQueryable _queryable;
    item[] _actualCollection = [new(4), new(3), new(2), new(1)];
    item[] _result;

    void Establish() => _queryable = _actualCollection.AsQueryable();

    void Because() => _result = [.. _queryable.OrderBy(nameof(item.Value), SortDirection.Ascending).Cast<item>()];

    [Fact] void should_be_ordered_correctly() => _result.ShouldEqual(_actualCollection.OrderBy(_ => _.Value));
}
