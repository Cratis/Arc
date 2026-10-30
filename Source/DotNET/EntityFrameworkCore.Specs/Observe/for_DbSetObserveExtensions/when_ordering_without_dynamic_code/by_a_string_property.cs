// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_ordering_without_dynamic_code;

public class by_a_string_property : an_ordering_without_dynamic_code
{
    string[] _result;

    void Because() => _result = [.. OrderBy(nameof(OrderingItem.Name), SortDirection.Ascending).Select(_ => _.Name)];

    [Fact] void should_sort_ascending() => _result.ShouldEqual(["a", "b", "c"]);
}
#endif
