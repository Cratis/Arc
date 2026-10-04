// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using SortDirection = Cratis.Arc.Queries.SortDirection;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_adding;

public class and_sort_values_tie : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeClassWithSomeId _lowerId;
    SomeClassWithSomeId _higherId;

    void Establish()
    {
        _set = new(QueryContextBuilder.New()
            .WithSorting(new(nameof(SomeClassWithSomeId.Value), SortDirection.Descending))
            .Build());
        _lowerId = new(new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), 42);
        _higherId = new(new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2), 42);
    }

    void Because()
    {
        _set.Add(_higherId);
        _set.Add(_lowerId);
    }

    [Fact] void should_order_by_id_ascending_like_the_server() => Assert.Equal([_lowerId, _higherId], _set);
}
