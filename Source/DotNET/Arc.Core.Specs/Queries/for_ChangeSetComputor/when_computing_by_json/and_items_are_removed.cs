// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ChangeSetComputor.when_computing_by_json;

public class and_items_are_removed : given.a_change_set_computor
{
    given.ItemWithoutId[] _previous;
    given.ItemWithoutId[] _current;
    ChangeSet _result;

    void Establish()
    {
        _previous =
        [
            new given.ItemWithoutId("First", 10),
            new given.ItemWithoutId("Second", 20)
        ];
        _current =
        [
            new given.ItemWithoutId("First", 10)
        ];
    }

    void Because() => _result = _computor.Compute(_previous, _current);

    [Fact] void should_have_empty_added() => _result.Added.ShouldBeEmpty();
    [Fact] void should_have_empty_replaced() => _result.Replaced.ShouldBeEmpty();
    [Fact] void should_report_missing_item_as_removed() => _result.Removed.Count().ShouldEqual(1);
    [Fact] void should_have_correct_removed_item() => _result.Removed.Cast<given.ItemWithoutId>().First().Name.ShouldEqual("Second");
}
