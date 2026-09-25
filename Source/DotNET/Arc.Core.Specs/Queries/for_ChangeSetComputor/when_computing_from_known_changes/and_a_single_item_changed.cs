// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_from_known_changes;

/// <summary>
/// A provider that watched its data source knows which item changed and says so, so the delta is the one it stated
/// rather than one rediscovered by comparing every item of the new snapshot against every item of the previous one.
/// </summary>
public class and_a_single_item_changed : given.a_change_set_computor
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
            new given.ItemWithId(_id2, "Second", 20),
            new given.ItemWithId(_id3, "Third", 30)
        ];
        _current =
        [
            _previous[0],
            new given.ItemWithId(_id2, "Second Updated", 99),
            _previous[2]
        ];
    }

    void Because() => _result = _computor.ComputeFromKnownChanges(
        [new CollectionChange(CollectionChangeKind.Replaced, _id2)],
        _current,
        _previous);

    [Fact] void should_report_nothing_added() => _result.Added.ShouldBeEmpty();
    [Fact] void should_report_nothing_removed() => _result.Removed.ShouldBeEmpty();
    [Fact] void should_report_only_the_changed_item_as_replaced() => _result.Replaced.Count().ShouldEqual(1);
    [Fact] void should_report_the_item_the_change_named() => _result.Replaced.Cast<given.ItemWithId>().First().Id.ShouldEqual(_id2);
    [Fact] void should_carry_the_current_value_of_the_item() => _result.Replaced.Cast<given.ItemWithId>().First().Value.ShouldEqual(99);
}
