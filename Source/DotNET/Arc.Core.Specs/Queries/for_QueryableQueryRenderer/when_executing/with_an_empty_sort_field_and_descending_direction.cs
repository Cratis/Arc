// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableQueryRenderer.when_executing;

public class with_an_empty_sort_field_and_descending_direction : Specification
{
    record item(string Name);

    QueryContext _context;
    Exception _result;

    void Establish() => _context = QueryContext.NotSet with { Sorting = new Sorting(string.Empty, SortDirection.Descending) };

    void Because() => _result = Catch.Exception(() => new QueryableQueryRenderer().Execute(new item[] { new("a") }.AsQueryable(), _context));

    [Fact] void should_throw_null_reference() => _result.ShouldBeOfExactType<NullReferenceException>();
}
