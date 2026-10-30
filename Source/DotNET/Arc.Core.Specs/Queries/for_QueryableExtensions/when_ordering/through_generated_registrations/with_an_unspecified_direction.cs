// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering.through_generated_registrations;

public class with_an_unspecified_direction : Specification
{
    record item(string Name);

    IQueryable _result;

    void Establish()
    {
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.RegisterSortKey<item, string>();
    }

    void Because() => _result = new item[] { new("a"), new("c"), new("b") }.AsQueryable().OrderBy("Name", SortDirection.Unspecified);

    [Fact] void should_order_descending() => ((MethodCallExpression)_result.Expression).Method.GetGenericMethodDefinition().ShouldEqual(Queryables.MethodDefinition(nameof(Queryable.OrderByDescending)));
    [Fact] void should_sort_descending() => _result.Cast<item>().Select(_ => _.Name).ShouldEqual(["c", "b", "a"]);
}
