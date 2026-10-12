// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound.for_ModelBoundQueryMethod;

public class when_selecting_queries : Specification
{
    MethodInfo[] _selected;
    MethodInfo[] _conventional;

    void Because()
    {
        _selected = Methods(typeof(given.SelectedReadModel));
        _conventional = Methods(typeof(given.ConventionalReadModel));
    }

    [Fact] void should_select_marked_queries_across_partials() => _selected.Select(_ => _.Name).ShouldContainOnly("ById", "Observe");
    [Fact] void should_choose_the_marked_overload() => _selected.Single(_ => _.Name == "ById").GetParameters()[0].ParameterType.ShouldEqual(typeof(string));
    [Fact] void should_leave_conventional_siblings_visible() => _conventional.Select(_ => _.Name).ShouldContainOnly("Marked", "Create", "Sibling");
    [Fact] void should_allow_zero_queries() => Methods(typeof(given.NoQueries)).ShouldBeEmpty();
    [Fact] void should_inherit_the_nearest_setting() => Methods(typeof(given.InheritedSelection)).Select(_ => _.Name).ShouldContainOnly("All");
    [Fact] void should_reset_at_a_direct_attribute() => Methods(typeof(given.ResetSelection)).Select(_ => _.Name).ShouldContainOnly("All");
    [Fact] void should_not_inherit_from_an_enclosing_type() => Methods(typeof(given.SelectionContainer.Nested)).Select(_ => _.Name).ShouldContainOnly("All");

    static MethodInfo[] Methods(Type type) => ModelBoundQueryMethod.GetMethods(type, method => method.IsValidQueryFor(type));
}
