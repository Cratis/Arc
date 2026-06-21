// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Helper utilities for running and testing source generators.
/// </summary>
public static class GeneratorTestHelper
{
    /// <summary>
    /// Runs the <see cref="QueryMetadataGenerator"/> against the supplied source text and
    /// returns the result.
    /// </summary>
    /// <param name="source">C# source code to compile and pass to the generator.</param>
    /// <returns>The <see cref="GeneratorDriverRunResult"/>.</returns>
    public static GeneratorDriverRunResult RunGenerator(string source)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var generator = new QueryMetadataGenerator();
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGenerators(compilation);

        return driver.GetRunResult();
    }

    /// <summary>
    /// Compiles the supplied source texts together and returns the resulting compilation diagnostics.
    /// </summary>
    /// <param name="sources">The C# source texts to compile in a single compilation.</param>
    /// <returns>The <see cref="Diagnostic"/> entries produced by the compilation.</returns>
    /// <remarks>
    /// Used to prove that generated output stays collision-proof when the same generator is loaded more than once
    /// in a single compilation — passing the same generated source twice must not produce a CS0101 duplicate
    /// definition error.
    /// </remarks>
    public static ImmutableArray<Diagnostic> Compile(params string[] sources)
    {
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            sources.Select(source => CSharpSyntaxTree.ParseText(source)),
            GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics();
    }

    /// <summary>
    /// Gets generated source text for a file with a specific hint name.
    /// </summary>
    /// <param name="result">The generator run result.</param>
    /// <param name="hintName">The exact generated file hint name.</param>
    /// <returns>The generated source, or an empty string if not found.</returns>
    public static string GetGeneratedSourceByHintName(GeneratorDriverRunResult result, string hintName)
    {
        var source = result.Results
            .SelectMany(_ => _.GeneratedSources)
            .FirstOrDefault(_ => _.HintName == hintName);

        return source.HintName is null ? string.Empty : source.SourceText.ToString();
    }

    static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var systemRuntime = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");

        return
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            MetadataReference.CreateFromFile(systemRuntime.Location),
            MetadataReference.CreateFromFile(typeof(Cratis.Arc.Queries.ModelBound.ReadModelAttribute).Assembly.Location),
        ];
    }
}
