// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class by_a_nullable_value_type_property : Specification
{
    record item(int? Score);

    IQueryable _result;
    LambdaExpression _lambda;

    void Because()
    {
        _result = new item[] { new(3), new(null), new(1) }.AsQueryable().OrderBy("Score");
        _lambda = Queryables.KeySelector((MethodCallExpression)_result.Expression);
    }

    [Fact] void should_have_key_selector_type() => _lambda.Type.ShouldEqual(typeof(Func<item, int?>));
    [Fact] void should_be_ordered_with_nulls_first() => _result.Cast<item>().Select(_ => _.Score).ShouldEqual([null, 1, 3]);
}
