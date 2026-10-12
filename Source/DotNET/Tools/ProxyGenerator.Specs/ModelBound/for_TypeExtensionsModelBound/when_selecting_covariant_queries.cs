// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod.given;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_TypeExtensionsModelBound;

public class when_selecting_covariant_queries : Specification
{
    MethodInfo[] _conventional;
    MethodInfo[] _marked;

    void Because()
    {
        _conventional = CovariantQueryType.Create(false).GetQueryMethods().ToArray();
        _marked = CovariantQueryType.Create(true).GetQueryMethods().ToArray();
    }

    [Fact] void should_select_the_conventional_covariant_query() => _conventional.Select(_ => _.Name).ShouldContainOnly("All");
    [Fact] void should_keep_the_covariant_query_valid_when_marked() => _marked.Select(_ => _.Name).ShouldContainOnly("All");
}
