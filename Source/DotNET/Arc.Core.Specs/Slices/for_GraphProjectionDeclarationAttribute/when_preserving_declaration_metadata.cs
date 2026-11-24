// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Slices.for_GraphProjectionDeclarationAttribute;

public class when_preserving_declaration_metadata : Specification
{
    readonly Type[] _members = [typeof(string), typeof(int)];
    GraphProjectionDeclarationAttribute _attribute;
    AttributeUsageAttribute _usage;

    void Because()
    {
        _attribute = new("PeopleDirectory", "People", _members);
        _members[0] = typeof(bool);
        _usage = typeof(GraphProjectionDeclarationAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
    }

    [Fact] void should_keep_the_name() => _attribute.Name.ShouldEqual("PeopleDirectory");
    [Fact] void should_keep_the_graph() => _attribute.Graph.ShouldEqual("People");
    [Fact] void should_snapshot_the_members() => _attribute.Members.ShouldEqual(typeof(string), typeof(int));
    [Fact] void should_expose_a_read_only_member_collection() => ((ICollection<Type>)_attribute.Members).IsReadOnly.ShouldBeTrue();
    [Fact] void should_only_target_classes() => _usage.ValidOn.ShouldEqual(AttributeTargets.Class);
    [Fact] void should_not_allow_multiple_markers() => _usage.AllowMultiple.ShouldBeFalse();
    [Fact] void should_not_inherit_the_marker() => _usage.Inherited.ShouldBeFalse();
}
