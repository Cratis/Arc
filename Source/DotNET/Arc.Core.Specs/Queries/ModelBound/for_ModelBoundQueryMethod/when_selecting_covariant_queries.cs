// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod;

public class when_selecting_covariant_queries : Specification
{
    MethodInfo[] _conventional;
    MethodInfo[] _marked;

    void Because()
    {
        var conventional = given.CovariantQueryType.Create(false);
        var marked = given.CovariantQueryType.Create(true);
        _conventional = ModelBoundQueryMethod.GetMethods(conventional, method => method.IsValidQueryFor(conventional));
        _marked = ModelBoundQueryMethod.GetMethods(marked, method => method.IsValidQueryFor(marked));
    }

    [Fact] void should_select_the_conventional_covariant_query() => _conventional.Select(_ => _.Name).ShouldContainOnly("All");
    [Fact] void should_keep_the_covariant_query_valid_when_marked() => _marked.Select(_ => _.Name).ShouldContainOnly("All");
}
