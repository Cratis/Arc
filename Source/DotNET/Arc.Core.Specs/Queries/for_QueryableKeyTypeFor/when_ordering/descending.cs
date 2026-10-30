// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.Queries.for_QueryableKeyTypeFor.when_ordering;

public class descending : Specification
{
    record Item(int Rank);

    int[] _result;

    void Because()
    {
        var parameter = Expression.Parameter(typeof(Item), "x");
        var body = Expression.Property(parameter, nameof(Item.Rank));
        _result = [.. new QueryableKeyTypeFor<int>().OrderBy(new[] { new Item(2), new Item(3), new Item(1) }.AsQueryable(), body, parameter, SortDirection.Descending).Select(_ => _.Rank)];
    }

    [Fact] void should_sort_descending() => _result.ShouldEqual([3, 2, 1]);
}
