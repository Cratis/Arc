// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_replacing_with_a_page;

public class and_documents_entered_left_and_changed : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeClassWithSomeId _kept;
    SomeClassWithSomeId _changed;
    SomeClassWithSomeId _left;
    SomeClassWithSomeId _entered;
    IReadOnlyList<CollectionChange>? _changes;

    void Establish()
    {
        _set = new(QueryContextBuilder.New().WithPageSize(3).Build());
        _kept = new(Guid.NewGuid(), 1);
        _changed = new(Guid.NewGuid(), 2);
        _left = new(Guid.NewGuid(), 3);
        _entered = new(Guid.NewGuid(), 4);
        _set.ReplaceWith([_kept, _changed, _left], (left, right) => left == right);
    }

    void Because() => _changes = _set.ReplaceWith([_kept, _changed with { Value = 5 }, _entered], (left, right) => left == right);

    [Fact] void should_report_each_change_by_id() => Assert.Equal(
        [
            new(CollectionChangeKind.Removed, _left.Id),
            new(CollectionChangeKind.Replaced, _changed.Id),
            new(CollectionChangeKind.Added, _entered.Id)
        ],
        _changes);
    [Fact] void should_hold_the_new_page() => Assert.Equal([_kept, _changed with { Value = 5 }, _entered], _set);
}
