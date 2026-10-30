// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_building_expressions;

public class skipping : Specification
{
    record item(int Value);

    IQueryable _queryable;
    MethodCallExpression _call;

    void Establish() => _queryable = new item[] { new(1), new(2), new(3) }.AsQueryable();

    void Because() => _call = (MethodCallExpression)_queryable.Skip(2).Expression;

    [Fact] void should_call_skip() => _call.Method.GetGenericMethodDefinition().ShouldEqual(Queryables.MethodDefinition(nameof(Queryable.Skip)));
    [Fact] void should_close_over_the_element_type() => _call.Method.GetGenericArguments().ShouldEqual([typeof(item)]);
    [Fact] void should_skip_the_source_expression() => _call.Arguments[0].ShouldBeSame(_queryable.Expression);
    [Fact] void should_pass_the_count_as_int_constant() => ((ConstantExpression)_call.Arguments[1]).Value.ShouldEqual(2);
}
