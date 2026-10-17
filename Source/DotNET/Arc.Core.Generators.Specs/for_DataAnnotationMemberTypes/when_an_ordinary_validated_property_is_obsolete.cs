// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes;

public class when_an_ordinary_validated_property_is_obsolete : given.diagnostic_sensitive_members
{
    void Because() => CompileProperty("[StringLength(3), Obsolete(\"x\", true)] public string Label { get; } = \"value\";");

    [Fact] void should_not_register_any_members_for_the_type() => _registered.ShouldBeFalse();
    [Fact] void should_compile_without_generated_diagnostics() => _diagnostics.ShouldBeEmpty();
}
