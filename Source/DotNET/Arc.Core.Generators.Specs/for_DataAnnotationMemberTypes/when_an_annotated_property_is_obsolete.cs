// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_an_annotated_property_is_obsolete : given.diagnostic_sensitive_members
{
    void Because() => CompileProperty("[Required, Obsolete(\"x\", true)] public Text Name { get; } = new(\"value\");");

    [Fact] void should_not_register_the_type() => _registered.ShouldBeFalse();
    [Fact] void should_compile_without_generated_diagnostics() => _diagnostics.ShouldBeEmpty();
    [Fact] void should_still_register_the_normal_concept_type() => _normalRegistered.ShouldBeTrue();
}
