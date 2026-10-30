// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering.through_generated_registrations;

public class and_building_the_key_selector : Specification
{
    record item(string Name);

    IQueryable _queryable;
    IQueryable _result;
    MethodCallExpression _call;
    LambdaExpression _lambda;

    void Establish()
    {
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.RegisterSortKey<item, string>();
        _queryable = new item[] { new("b"), new("a") }.AsQueryable();
    }

    void Because()
    {
        _result = _queryable.OrderBy("Name", SortDirection.Ascending);
        _call = (MethodCallExpression)_result.Expression;
        _lambda = Queryables.KeySelector(_call);
    }

    [Fact] void should_call_order_by() => _call.Method.GetGenericMethodDefinition().ShouldEqual(Queryables.MethodDefinition(nameof(Queryable.OrderBy)));
    [Fact] void should_close_over_element_and_key_type() => _call.Method.GetGenericArguments().ShouldEqual([typeof(item), typeof(string)]);
    [Fact] void should_order_the_source_expression() => _call.Arguments[0].ShouldBeSame(_queryable.Expression);
    [Fact] void should_quote_the_key_selector() => _call.Arguments[1].NodeType.ShouldEqual(ExpressionType.Quote);
    [Fact] void should_have_key_selector_type() => _lambda.Type.ShouldEqual(typeof(Func<item, string>));
    [Fact] void should_have_a_single_parameter_named_x() => _lambda.Parameters.Select(_ => (_.Name, _.Type)).ShouldEqual([("x", typeof(item))]);
    [Fact] void should_select_the_property_of_the_parameter() => ((MemberExpression)_lambda.Body).Expression.ShouldBeSame(_lambda.Parameters[0]);
    [Fact] void should_select_the_property_info() => ((MemberExpression)_lambda.Body).Member.ShouldEqual(typeof(item).GetProperty("Name"));
    [Fact] void should_reflect_the_element_type() => ((MemberExpression)_lambda.Body).Member.ReflectedType.ShouldEqual(typeof(item));
    [Fact] void should_be_ordered() => _result.Cast<item>().Select(_ => _.Name).ShouldEqual(["a", "b"]);
}
