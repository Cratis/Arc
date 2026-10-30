// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering.descending;

public class and_building_the_key_selector : Specification
{
    record item(string Name);

    IQueryable _queryable;
    MethodCallExpression _call;
    LambdaExpression _lambda;

    void Establish() => _queryable = new item[] { new("a"), new("b") }.AsQueryable();

    void Because()
    {
        _call = (MethodCallExpression)_queryable.OrderBy("Name", SortDirection.Descending).Expression;
        _lambda = Queryables.KeySelector(_call);
    }

    [Fact] void should_call_order_by_descending() => _call.Method.GetGenericMethodDefinition().ShouldEqual(Queryables.MethodDefinition(nameof(Queryable.OrderByDescending)));
    [Fact] void should_close_over_element_and_key_type() => _call.Method.GetGenericArguments().ShouldEqual([typeof(item), typeof(string)]);
    [Fact] void should_have_key_selector_type() => _lambda.Type.ShouldEqual(typeof(Func<item, string>));
    [Fact] void should_have_a_single_parameter_named_x() => _lambda.Parameters.Select(_ => _.Name).ShouldEqual(["x"]);
    [Fact] void should_select_the_property_info() => ((MemberExpression)_lambda.Body).Member.ShouldEqual(typeof(item).GetProperty("Name"));
}
