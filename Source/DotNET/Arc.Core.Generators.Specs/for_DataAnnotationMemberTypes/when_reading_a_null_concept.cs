// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_reading_a_null_concept : given.annotated_models
{
    object? _value;

    void Because()
    {
        Compile(Models);
        var type = _assembly.GetType("Annotated.Submit")!;
        DataAnnotationMembers.TryGet(type, out var members);
        var text = _assembly.GetType("Annotated.Text")!;
        var command = Activator.CreateInstance(type, null, "label", Activator.CreateInstance(text, "other"), "plain")!;
        _value = members!.Single(member => member.Name == "Name").ReadValue(command);
    }

    [Fact] void should_return_null_without_dereferencing_the_concept() => _value.ShouldBeNull();
}
