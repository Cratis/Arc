// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_CommandMetadataGenerator;

public class when_assembly_has_no_commands : Specification
{
    GeneratorDriverRunResult _result;

    void Because()
    {
        var compilation = CSharpCompilation.Create("NoCommands", [CSharpSyntaxTree.ParseText("public record NotACommand;")]);
        _result = CSharpGeneratorDriver.Create(new CommandMetadataGenerator()).RunGenerators(compilation).GetRunResult();
    }

    [Fact] void should_not_generate_metadata() => _result.GeneratedTrees.ShouldBeEmpty();
    [Fact] void should_not_report_generator_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
}
