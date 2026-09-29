// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Cratis.Arc.Generators.Specs.for_ModelGraphWalkerGenerator;

/// <summary>
/// Compiles model source, with or without the <see cref="ModelGraphWalkerGenerator"/>, the way a consuming project is.
/// </summary>
public static class ModelGraphWalkerCompilation
{
    /// <summary>
    /// Compiles source, optionally runs the generator over it, and checks the result builds.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The model source.</param>
    /// <param name="generate">Whether to run the generator.</param>
    /// <param name="references">Additional references.</param>
    /// <returns>The generated walker source, if any, the warnings and errors outside the model source, and the compilation.</returns>
    public static (string Source, Diagnostic[] Diagnostics, Compilation Output) Compile(string name, string source, bool generate, params MetadataReference[] references)
    {
        var compilation = CSharpCompilation.Create(
            name,
            [CSharpSyntaxTree.ParseText(source)],
            References().Concat(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        if (!generate)
        {
            return (string.Empty, [], compilation);
        }

        var driver = CSharpGeneratorDriver.Create(new ModelGraphWalkerGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        var generated = driver.GetRunResult().Results.Single().GeneratedSources
            .SingleOrDefault(generatedSource => generatedSource.HintName == "ModelGraphWalkers.g.cs");
        var models = compilation.SyntaxTrees.Single();
        Diagnostic[] diagnostics = [.. generatorDiagnostics, .. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning && diagnostic.Location.SourceTree != models)];
        return (generated.HintName is null ? string.Empty : generated.SourceText.ToString(), diagnostics, output);
    }

    /// <summary>
    /// Emits a library as a reference, either as a reference assembly or as its implementation.
    /// </summary>
    /// <param name="name">The assembly name.</param>
    /// <param name="source">The library source.</param>
    /// <param name="referenceAssembly">Whether to emit a reference assembly.</param>
    /// <returns>The <see cref="MetadataReference"/>.</returns>
    /// <exception cref="InvalidOperationException">The library does not compile.</exception>
    public static MetadataReference Library(string name, string source, bool referenceAssembly)
    {
        var (_, _, compilation) = Compile(name, source, false);
        using var binary = new MemoryStream();
        var emitted = compilation.Emit(binary, options: new EmitOptions(metadataOnly: referenceAssembly, includePrivateMembers: !referenceAssembly));
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        }

        return MetadataReference.CreateFromImage(binary.ToArray());
    }

    /// <summary>
    /// Emits a compilation, loads it and runs its module initializers.
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The loaded <see cref="Assembly"/>.</returns>
    /// <exception cref="InvalidOperationException">The compilation does not emit.</exception>
    public static Assembly Load(Compilation compilation)
    {
        using var binary = new MemoryStream();
        var emitted = compilation.Emit(binary);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)));
        }

        var assembly = Assembly.Load(binary.ToArray());
        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
        return assembly;
    }

    static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ModelGraphWalkers).Assembly.Location)
            .Append(typeof(ConceptAs<>).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path));
}
