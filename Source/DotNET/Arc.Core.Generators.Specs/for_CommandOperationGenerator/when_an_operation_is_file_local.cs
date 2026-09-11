// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_CommandOperationGenerator;

public class when_an_operation_is_file_local : Specification
{
    string _source;
    Diagnostic[] _errors;

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "FileLocalOperationSpec",
            [CSharpSyntaxTree.ParseText("using Cratis.Arc.Commands; file record LocalOperation : ICommandOperation { public void Execute() { } }")],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(ICommandOperation).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var driver = CSharpGeneratorDriver.Create(new CommandOperationGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        _source = driver.GetRunResult().Results.Single().GeneratedSources.Single().SourceText.ToString();
        _errors = [.. diagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];
    }

    [Fact] void should_not_emit_an_inaccessible_invocation() => _source.Contains("LocalOperation", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_the_compilation_valid() => _errors.ShouldBeEmpty();
}
