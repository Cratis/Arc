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
            MetadataReference.CreateFromFile(systemRuntime!.Location),
            MetadataReference.CreateFromFile(typeof(Cratis.Arc.Queries.ModelBound.ReadModelAttribute).Assembly.Location),
        ];
    }
}
