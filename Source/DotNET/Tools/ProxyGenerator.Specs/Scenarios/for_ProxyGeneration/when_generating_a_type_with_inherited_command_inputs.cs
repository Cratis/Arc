// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class when_generating_a_type_with_inherited_command_inputs : Specification
{
    string _generatedCode;

    void Because() => _generatedCode = InMemoryProxyGenerator.GenerateType(typeof(CommandInputsMiddle).ToTypeDescriptor("/output", 5));

    [Fact] void should_preserve_type_inheritance() => _generatedCode.ShouldContain("extends CommandInputsBase");
    [Fact] void should_emit_declared_properties() => _generatedCode.ShouldContain("count!: number");
    [Fact] void should_not_flatten_inherited_properties() => _generatedCode.ShouldNotContain("name!");
    [Fact] void should_not_flatten_inherited_complex_properties() => _generatedCode.ShouldNotContain("details!");
}
