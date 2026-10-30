// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_ordering_without_dynamic_code;

public class by_an_int_property_descending : an_ordering_without_dynamic_code
{
    int[] _result;

    void Because() => _result = [.. OrderBy(nameof(OrderingItem.Rank), SortDirection.Descending).Select(_ => _.Rank)];

    [Fact] void should_sort_descending() => _result.ShouldEqual([3, 2, 1]);
}
#endif
