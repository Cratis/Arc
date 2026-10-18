// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_generating_inherited_properties : given.annotated_models
{
    DataAnnotationMember[] _members;
    Func<ValidationAttribute[]> _attributes;
    object _command;

    void Because()
    {
        Compile(Models);
        var type = _assembly.GetType("Annotated.Derived")!;
        DataAnnotationMembers.TryGet(type, out _members!, out _attributes!);
        _command = Activator.CreateInstance(type, Activator.CreateInstance(_assembly.GetType("Annotated.Text")!, "valid"))!;
    }

    [Fact] void should_compile_without_generated_warnings() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_include_the_inherited_property() => _members.Select(member => member.Name).ShouldContainOnly("Name", "Label");
    [Fact] void should_read_the_inherited_value() => _members.Single(member => member.Name == "Label").ReadValue(_command).ShouldEqual("long");
    [Fact] void should_read_the_inherited_attributes() => _members.Single(member => member.Name == "Label").Attributes().Single().ShouldBeOfExactType<StringLengthAttribute>();
    [Fact] void should_include_class_level_attributes() => _attributes().Single().IsValid(_command).ShouldBeFalse();
}
