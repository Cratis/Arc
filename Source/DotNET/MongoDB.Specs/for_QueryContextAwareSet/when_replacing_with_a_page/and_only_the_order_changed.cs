// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_replacing_with_a_page;

public class and_only_the_order_changed : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeClassWithSomeId _first;
    SomeClassWithSomeId _second;
    IReadOnlyList<CollectionChange>? _changes;

    void Establish()
    {
        _set = new(QueryContextBuilder.New().WithPageSize(2).Build());
        _first = new(Guid.NewGuid(), 1);
        _second = new(Guid.NewGuid(), 2);
        _set.ReplaceWith([_first, _second], (left, right) => left == right);
    }

    void Because() => _changes = _set.ReplaceWith([_second, _first], (left, right) => left == right);

    [Fact] void should_report_a_change_without_entries() => _changes.ShouldBeEmpty();
    [Fact] void should_hold_the_new_order() => Assert.Equal([_second, _first], _set);
}
