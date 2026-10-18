// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_generating_annotated_properties : given.annotated_models
{
    DataAnnotationMember[] _members;
    object _command;

    void Because()
    {
        Compile(Models);
        var type = _assembly.GetType("Annotated.Submit")!;
        DataAnnotationMembers.TryGet(type, out _members!);
        var text = _assembly.GetType("Annotated.Text")!;
        _command = Activator.CreateInstance(type, Activator.CreateInstance(text, "value"), "label", Activator.CreateInstance(text, "other"), "plain")!;
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_register_only_annotated_members() => _members.Select(member => member.Name).ShouldContainOnly("Name", "Label");
    [Fact] void should_unwrap_the_concept() => _members.Single(member => member.Name == "Name").ReadValue(_command).ShouldEqual("value");
    [Fact] void should_read_the_non_concept_directly() => _members.Single(member => member.Name == "Label").ReadValue(_command).ShouldEqual("label");
    [Fact] void should_honor_the_display_name() => _members.Single(member => member.Name == "Name").DisplayName.ShouldEqual("Customer name");
    [Fact] void should_read_the_attributes_lazily() => _members.Single(member => member.Name == "Name").Attributes().Length.ShouldEqual(1);
    [Fact] void should_emit_a_typed_value_read() => _source.ShouldContain("((global::Annotated.Submit)instance).@Name is { } concept ? (object?)concept.Value : null");
}
