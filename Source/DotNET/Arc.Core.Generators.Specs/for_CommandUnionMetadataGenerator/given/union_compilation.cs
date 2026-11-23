// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.for_CommandUnionMetadataGenerator.given;

public static class union_compilation
{
    public static CSharpCompilation Create(string source, string name, params MetadataReference[] references) => CSharpCompilation.Create(
        name,
        [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(CommandResult).Assembly.Location).Append(typeof(CorrelationId).Assembly.Location)
            .Append(typeof(OneOf.OneOf<,>).Assembly.Location).Append(typeof(Cratis.Monads.Result<,>).Assembly.Location)
            .Distinct(StringComparer.Ordinal).Select(path => MetadataReference.CreateFromFile(path)).Concat(references),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    public static (Compilation Output, string Source, Diagnostic[] Diagnostics) Generate(CSharpCompilation compilation)
    {
        var driver = CSharpGeneratorDriver.Create([new CommandUnionMetadataGenerator().AsSourceGenerator()], parseOptions: new CSharpParseOptions(LanguageVersion.Preview))
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        var source = string.Join('\n', driver.GetRunResult().Results.Single().GeneratedSources.Select(source => source.SourceText.ToString()));
        return (output, source, [.. diagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning)]);
    }

    public static byte[] Emit(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success) throw new InvalidOperationException(string.Join(Environment.NewLine, result.Diagnostics));
        return stream.ToArray();
    }

    public static Assembly LoadAndRegister(byte[] binary)
    {
        var assembly = Assembly.Load(binary);
        GeneratedMetadataRegistration.Default.Register(
            typeof(union_compilation).Assembly,
            [new(assembly.GetName().Name!, () => assembly.ManifestModule)],
            []);
        GeneratedMetadataRegistration.EnsureGeneratedMetadataRegistered();
        return assembly;
    }
}
