// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod;

public class when_selecting_public_queries : Specification
{
    MethodInfo[] _selected;
    MethodInfo[] _valid;

    void Because()
    {
        var owner = typeof(given.QueryVisibilityReadModel);
        _selected = ModelBoundQueryMethod.GetMethods(owner, method => method.IsValidQueryFor(owner));
        _valid = owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => method.IsValidQueryFor(owner)).ToArray();
    }

    [Fact] void should_select_only_the_public_method() => _selected.Select(method => method.Name).ShouldContainOnly("Public");
    [Fact] void should_reject_nonpublic_methods_in_the_runtime_predicate() => _valid.Select(method => method.Name).ShouldContainOnly("Public");
}
