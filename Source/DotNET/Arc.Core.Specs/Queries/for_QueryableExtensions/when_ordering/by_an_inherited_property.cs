// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Arc.Queries.for_QueryableExtensions.given;

namespace Cratis.Arc.Queries.for_QueryableExtensions.when_ordering;

public class by_an_inherited_property : Specification
{
    class base_item
    {
        public int Rank { get; init; }
    }

    class derived_item : base_item;

    IQueryable _result;
    MethodCallExpression _call;
    MemberExpression _member;

    void Because()
    {
        _result = new derived_item[] { new() { Rank = 2 }, new() { Rank = 1 } }.AsQueryable().OrderBy("Rank");
        _call = (MethodCallExpression)_result.Expression;
        _member = (MemberExpression)Queryables.KeySelector(_call).Body;
    }

    [Fact] void should_close_over_element_and_key_type() => _call.Method.GetGenericArguments().ShouldEqual([typeof(derived_item), typeof(int)]);
    [Fact] void should_select_the_base_declared_property() => _member.Member.DeclaringType.ShouldEqual(typeof(base_item));
    [Fact] void should_reflect_the_element_type() => _member.Member.ReflectedType.ShouldEqual(typeof(derived_item));
    [Fact] void should_be_ordered() => _result.Cast<derived_item>().Select(_ => _.Rank).ShouldEqual([1, 2]);
}
