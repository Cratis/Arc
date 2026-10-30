// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_QueryableQueryRenderer.when_executing;

public class without_sorting_or_paging : Specification
{
    record item(string Name);

    QueryableQueryRenderer _renderer;
    IQueryable _query;
    QueryRendererResult _result;

    void Establish()
    {
        _renderer = new();
        _query = new item[] { new("a"), new("b"), new("c") }.AsQueryable();
    }

    void Because() => _result = _renderer.Execute(_query, QueryContext.NotSet);

    [Fact] void should_count_the_items() => _result.TotalItems.ShouldEqual(3);
    [Fact] void should_return_the_query_unchanged() => _result.Data.ShouldBeSame(_query);
}
