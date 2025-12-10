// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableExtensions;

public class when_skipping : Specification
{
    IQueryable _queryable;
    int[] _actualCollection = [1, 2, 3, 4];
    int[] _result;

    void Establish() => _queryable = _actualCollection.AsQueryable();

    void Because() => _result = [.. _queryable.Skip(2).Cast<int>()];

    [Fact] void should_contain_expected_items() => _result.ShouldContainOnly(_actualCollection.Skip(2));
}
