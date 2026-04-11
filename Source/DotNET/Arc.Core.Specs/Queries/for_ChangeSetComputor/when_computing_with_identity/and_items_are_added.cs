// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_with_identity;

public class and_items_are_added : given.a_change_set_computor
{
    static readonly Guid _id1 = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    static readonly Guid _id2 = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);
    static readonly Guid _id3 = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

    given.ItemWithId[] _previous;
    given.ItemWithId[] _current;
    ChangeSet _result;

    void Establish()
    {
        _previous =
        [
            new given.ItemWithId(_id1, "First", 10),
            new given.ItemWithId(_id2, "Second", 20)
        ];
        _current =
        [
            new given.ItemWithId(_id1, "First", 10),
            new given.ItemWithId(_id2, "Second", 20),
            new given.ItemWithId(_id3, "Third", 30)
        ];
    }

    void Because() => _result = _computor.Compute(_previous, _current);

    [Fact] void should_report_new_item_as_added() => _result.Added.Count().ShouldEqual(1);
    [Fact] void should_have_empty_replaced() => _result.Replaced.ShouldBeEmpty();
    [Fact] void should_have_empty_removed() => _result.Removed.ShouldBeEmpty();
    [Fact] void should_have_correct_added_id() => _result.Added.Cast<given.ItemWithId>().First().Id.ShouldEqual(_id3);
}
