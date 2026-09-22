// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_from_known_changes;

/// <summary>
/// Removing an item from a paged query pulls the next one onto the page, so the emission states two changes. A delta
/// that mentioned only the removal would leave the client one row short of what the page now holds.
/// </summary>
public class and_a_paged_removal_refilled_the_page : given.a_change_set_computor
{
    static readonly Guid _removed = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    static readonly Guid _kept = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);
    static readonly Guid _refilled = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3);

    given.ItemWithId[] _previous;
    given.ItemWithId[] _current;
    ChangeSet _result;

    void Establish()
    {
        _previous =
        [
            new given.ItemWithId(_removed, "Going", 10),
            new given.ItemWithId(_kept, "Staying", 20)
        ];
        _current =
        [
            _previous[1],
            new given.ItemWithId(_refilled, "Pulled onto the page", 30)
        ];
    }

    void Because() => _result = _computor.ComputeFromKnownChanges(
        [
            new CollectionChange(CollectionChangeKind.Removed, _removed),
            new CollectionChange(CollectionChangeKind.Added, _refilled)
        ],
        _current,
        _previous);

    [Fact] void should_report_the_refill_as_added() => _result.Added.Cast<given.ItemWithId>().Single().Id.ShouldEqual(_refilled);
    [Fact] void should_report_the_removal() => _result.Removed.Cast<given.ItemWithId>().Single().Id.ShouldEqual(_removed);
    [Fact] void should_report_nothing_replaced() => _result.Replaced.ShouldBeEmpty();
}
