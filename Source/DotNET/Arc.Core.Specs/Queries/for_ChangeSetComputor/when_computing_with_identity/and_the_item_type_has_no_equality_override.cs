// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_with_identity;

/// <summary>
/// Reference and value equality settle almost every comparison, but an item type that overrides neither must still
/// have its content compared - otherwise a genuine change to such a type would be silently dropped from the delta.
/// </summary>
public class and_the_item_type_has_no_equality_override : given.a_change_set_computor
{
    static readonly Guid _unchangedId = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
    static readonly Guid _changedId = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2);

    given.MutableItemWithId[] _previous;
    given.MutableItemWithId[] _current;
    ChangeSet _result;

    void Establish()
    {
        _previous =
        [
            new() { Id = _unchangedId, Name = "First", Value = 10 },
            new() { Id = _changedId, Name = "Second", Value = 20 }
        ];
        _current =
        [
            new() { Id = _unchangedId, Name = "First", Value = 10 },
            new() { Id = _changedId, Name = "Second", Value = 99 }
        ];
    }

    void Because() => _result = _computor.Compute(_previous, _current);

    [Fact] void should_report_the_changed_item_as_replaced() => _result.Replaced.Cast<given.MutableItemWithId>().Single().Id.ShouldEqual(_changedId);
    [Fact] void should_not_report_the_equal_item_as_replaced() => _result.Replaced.Count().ShouldEqual(1);
    [Fact] void should_report_nothing_added() => _result.Added.ShouldBeEmpty();
    [Fact] void should_report_nothing_removed() => _result.Removed.ShouldBeEmpty();
}
