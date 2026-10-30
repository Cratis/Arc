// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;
using Cratis.Arc.Queries;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.when_ordering_without_dynamic_code;

public class by_an_unregistered_value_type_property : an_ordering_without_dynamic_code
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => OrderBy(nameof(OrderingItem.Mark), SortDirection.Ascending));

    [Fact] void should_throw_missing_queryable_sort_key() => _exception.ShouldBeOfExactType<MissingQueryableSortKey>();
    [Fact] void should_name_the_key_type() => _exception.Message.ShouldContain(typeof(OrderingStruct).ToString());
}
#endif
