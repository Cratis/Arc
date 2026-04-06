// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_first_update;

public class and_collection_has_items : given.a_change_set_computor
{
    static readonly Guid _id1 = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    static readonly Guid _id2 = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);

    given.ItemWithId[] _items;
    ChangeSet _result;

    void Establish() =>
        _items =
        [
            new given.ItemWithId(_id1, "First", 10),
            new given.ItemWithId(_id2, "Second", 20)
        ];

    void Because() => _result = _computor.Compute(null, _items);

    [Fact] void should_report_all_items_as_added() => _result.Added.Count().ShouldEqual(2);
    [Fact] void should_have_empty_replaced() => _result.Replaced.ShouldBeEmpty();
    [Fact] void should_have_empty_removed() => _result.Removed.ShouldBeEmpty();
}
