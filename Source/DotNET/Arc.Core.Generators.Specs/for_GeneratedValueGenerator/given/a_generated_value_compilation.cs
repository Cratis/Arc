// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Concepts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_GeneratedValueGenerator.given;

public class a_generated_value_compilation : Specification
{
    protected string _input;
    protected string _source;
    protected Diagnostic[] _diagnostics;
    protected Assembly _assembly;

    protected void Generate()
    {
        var compilation = CSharpCompilation.Create(
            $"GeneratedValueSpec{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(_input)],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Append(typeof(GeneratedValueAttribute).Assembly.Location)
                .Append(typeof(IGeneratable<>).Assembly.Location)
                .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        var driver = CSharpGeneratorDriver.Create(new GeneratedValueGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        _source = string.Join(Environment.NewLine, driver.GetRunResult().Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
        _diagnostics = [.. generatorDiagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)];
        using var binary = new MemoryStream();
        output.Emit(binary).Success.ShouldBeTrue();
        _assembly = Assembly.Load(binary.ToArray());
    }

    protected object Create(string commandName)
    {
        var commandType = _assembly.GetType(commandName)!;
        var parameters = GeneratedValueParameters.Describe(commandType, commandType.GetMethod("Handle")!, dynamicCodeSupported: false);
        return GeneratedValueFactories.Create(commandType, parameters.Single());
    }
}
