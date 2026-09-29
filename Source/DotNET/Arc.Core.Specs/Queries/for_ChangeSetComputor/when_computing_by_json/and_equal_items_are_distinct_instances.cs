// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_by_json;

public class and_equal_items_are_distinct_instances : given.a_change_set_computor
{
    ChangeSet _result;

    void Because() => _result = _computor.Compute(
        [new given.DerivedItemWithoutId { Name = "Same", Extra = 1 }],
        [new given.DerivedItemWithoutId { Name = "Same", Extra = 1 }]);

    [Fact] void should_have_empty_added() => _result.Added.ShouldBeEmpty();
    [Fact] void should_have_empty_removed() => _result.Removed.ShouldBeEmpty();
    [Fact] void should_have_empty_replaced() => _result.Replaced.ShouldBeEmpty();
}
