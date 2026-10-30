// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering.through_generated_registrations;

public class and_building_the_descending_key_selector : Specification
{
    record item(string Name);

    MethodCallExpression _call;
    LambdaExpression _lambda;
    IQueryable _result;

    void Establish()
    {
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.RegisterSortKey<item, string>();
    }

    void Because()
    {
        _result = new item[] { new("a"), new("b") }.AsQueryable().OrderBy("Name", SortDirection.Descending);
        _call = (MethodCallExpression)_result.Expression;
        _lambda = Queryables.KeySelector(_call);
    }

    [Fact] void should_call_order_by_descending() => _call.Method.GetGenericMethodDefinition().ShouldEqual(Queryables.MethodDefinition(nameof(Queryable.OrderByDescending)));
    [Fact] void should_close_over_element_and_key_type() => _call.Method.GetGenericArguments().ShouldEqual([typeof(item), typeof(string)]);
    [Fact] void should_have_key_selector_type() => _lambda.Type.ShouldEqual(typeof(Func<item, string>));
    [Fact] void should_select_the_property_info() => ((MemberExpression)_lambda.Body).Member.ShouldEqual(typeof(item).GetProperty("Name"));
    [Fact] void should_be_ordered() => _result.Cast<item>().Select(_ => _.Name).ShouldEqual(["b", "a"]);
}
