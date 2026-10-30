// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering.through_generated_registrations;

public class by_a_value_type_property : Specification
{
    record item(int Rank);

    IQueryable _result;
    LambdaExpression _lambda;

    void Establish()
    {
        QueryableElementTypes.Register<item>();
        QueryableElementTypes.RegisterSortKey<item, int>();
    }

    void Because()
    {
        _result = new item[] { new(3), new(1), new(2) }.AsQueryable().OrderBy("Rank");
        _lambda = Queryables.KeySelector((MethodCallExpression)_result.Expression);
    }

    [Fact] void should_have_key_selector_type() => _lambda.Type.ShouldEqual(typeof(Func<item, int>));
    [Fact] void should_select_the_member_without_conversion() => _lambda.Body.NodeType.ShouldEqual(ExpressionType.MemberAccess);
    [Fact] void should_be_ordered() => _result.Cast<item>().Select(_ => _.Rank).ShouldEqual([1, 2, 3]);
}
