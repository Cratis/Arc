// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_QueryContextAwareSet.when_replacing_with_a_page;

public class given_the_same_page : Specification
{
    QueryContextAwareSet<SomeClassWithSomeId> _set;
    SomeClassWithSomeId[] _page;
    IReadOnlyList<CollectionChange>? _changes;

    void Establish()
    {
        _set = new(QueryContextBuilder.New().WithPageSize(2).Build());
        _page = [new(Guid.NewGuid(), 1), new(Guid.NewGuid(), 2)];
        _set.ReplaceWith(_page, (left, right) => left == right);
    }

    void Because() => _changes = _set.ReplaceWith(_page, (left, right) => left == right);

    [Fact] void should_report_no_change() => _changes.ShouldBeNull();
    [Fact] void should_keep_the_page() => Assert.Equal(_page, _set);
}
