// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_with_identity;

public class and_the_identity_is_implemented_explicitly : given.a_change_set_computor
{
    static readonly Guid _keptId = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    static readonly Guid _removedId = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);
    static readonly Guid _addedId = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

    ChangeSet _result;

    void Because() => _result = _computor.Compute(
        [new given.ItemWithExplicitId(_keptId, "Before"), new given.ItemWithExplicitId(_removedId, "Removed")],
        [new given.ItemWithExplicitId(_keptId, "After"), new given.ItemWithExplicitId(_addedId, "Added")]);

    [Fact] void should_report_the_changed_item_as_replaced() => _result.Replaced.Cast<given.ItemWithExplicitId>().Single().Name.ShouldEqual("After");
    [Fact] void should_report_the_new_item_as_added() => _result.Added.Cast<given.ItemWithExplicitId>().Single().Name.ShouldEqual("Added");
    [Fact] void should_report_the_missing_item_as_removed() => _result.Removed.Cast<given.ItemWithExplicitId>().Single().Name.ShouldEqual("Removed");
}
