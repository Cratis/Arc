// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_by_json;

/// <summary>
/// Items are compared by the JSON of their runtime type, so a difference only a derived type carries is still seen.
/// </summary>
public class and_derived_items_differ_only_in_derived_properties : given.a_change_set_computor
{
    given.DerivedItemWithoutId _before;
    given.DerivedItemWithoutId _after;
    ChangeSet _result;

    void Establish()
    {
        _before = new() { Name = "Same", Extra = 1 };
        _after = new() { Name = "Same", Extra = 2 };
    }

    void Because() => _result = _computor.Compute([_before], [_after]);

    [Fact] void should_report_the_new_item_as_added() => _result.Added.Single().ShouldEqual(_after);
    [Fact] void should_report_the_old_item_as_removed() => _result.Removed.Single().ShouldEqual(_before);
    [Fact] void should_have_empty_replaced() => _result.Replaced.ShouldBeEmpty();
}
