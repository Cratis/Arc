// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_a_validated_property_hides_a_different_type : given.hidden_validated_properties
{
    async Task Because() => await CompileHiding("public int Name { get; } = 1;", "[Required] public new Text Name { get; } = new(\"value\");");

    [Fact] void should_not_register_the_type() => _registered.ShouldBeFalse();
    [Fact] void should_compile_without_generated_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_not_throw_during_initialization_or_validation() => _error.ShouldBeNull();
    [Fact] void should_keep_the_existing_object_validation_path() => _result.IsValid.ShouldBeTrue();
}
