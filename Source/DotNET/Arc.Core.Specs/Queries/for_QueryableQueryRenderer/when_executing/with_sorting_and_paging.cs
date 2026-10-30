// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Arc.Queries.for_QueryableQueryRenderer.when_executing;

public class with_sorting_and_paging : Specification
{
    record item(string Name);

    QueryableQueryRenderer _renderer;
    IQueryable _query;
    QueryContext _context;
    QueryRendererResult _result;

    void Establish()
    {
        _renderer = new();
        _query = new item[] { new("a"), new("b"), new("c"), new("d"), new("e") }.AsQueryable();
        _context = QueryContext.NotSet with { Paging = new Paging(1, 2, true), Sorting = new Sorting("Name", SortDirection.Descending) };
    }

    void Because() => _result = _renderer.Execute(_query, _context);

    [Fact] void should_count_before_paging() => _result.TotalItems.ShouldEqual(5);
    [Fact] void should_sort_skip_and_take() => ((IQueryable)_result.Data).Cast<item>().Select(_ => _.Name).ShouldEqual(["c", "b"]);
    [Fact] void should_end_the_expression_with_take() => ((MethodCallExpression)((IQueryable)_result.Data).Expression).Method.Name.ShouldEqual(nameof(Queryable.Take));
    [Fact] void should_have_skip_below_take() => ((MethodCallExpression)((MethodCallExpression)((IQueryable)_result.Data).Expression).Arguments[0]).Method.Name.ShouldEqual(nameof(Queryable.Skip));
    [Fact] void should_have_order_by_descending_below_skip() => ((MethodCallExpression)((MethodCallExpression)((MethodCallExpression)((IQueryable)_result.Data).Expression).Arguments[0]).Arguments[0]).Method.Name.ShouldEqual(nameof(Queryable.OrderByDescending));
}
