// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Runs the <see cref="ProjectReferenceModulesGenerator"/> over a compilation referencing in-memory project libraries.
/// </summary>
public static class ProjectReferenceModulesGeneratorRunner
{
    /// <summary>
    /// The source of the executable's entry point.
    /// </summary>
    public const string ProgramSource = "static class Program { static void Main() { } }";

    /// <summary>
    /// Gets references to the platform assemblies and to Cratis.Arc.Core.
    /// </summary>
    public static IEnumerable<MetadataReference> PlatformReferences { get; } =
        [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(ProjectReferenceModuleInitializers).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))];

    /// <summary>
    /// Runs the generator.
    /// </summary>
    /// <param name="outputKind">The <see cref="OutputKind"/> of the compilation.</param>
    /// <param name="projectReferences">The value of the <c>CratisArcProjectReferences</c> build property, or null when it is not reported.</param>
    /// <param name="libraries">The project libraries the compilation references.</param>
    /// <returns>The <see cref="ProjectReferenceModulesGeneratorResult"/>.</returns>
    public static ProjectReferenceModulesGeneratorResult Run(OutputKind outputKind, string? projectReferences, params ProjectLibrary[] libraries) =>
        Run(outputKind, projectReferences, LanguageVersion.Latest, libraries.Select(_ => _.Reference));

    /// <summary>
    /// Runs the generator.
    /// </summary>
    /// <param name="outputKind">The <see cref="OutputKind"/> of the compilation.</param>
    /// <param name="projectReferences">The value of the <c>CratisArcProjectReferences</c> build property, or null when it is not reported.</param>
    /// <param name="languageVersion">The C# <see cref="LanguageVersion"/> of the compilation.</param>
    /// <param name="references">The references to the project libraries.</param>
    /// <returns>The <see cref="ProjectReferenceModulesGeneratorResult"/>.</returns>
    public static ProjectReferenceModulesGeneratorResult Run(
        OutputKind outputKind,
        string? projectReferences,
        LanguageVersion languageVersion,
        IEnumerable<MetadataReference> references)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);
        var compilation = CreateCompilation(outputKind, parseOptions, outputKind == OutputKind.DynamicallyLinkedLibrary ? string.Empty : ProgramSource, references);
        var driver = CreateDriver(projectReferences, parseOptions)
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);

        var generated = driver.GetRunResult().Results
            .SelectMany(_ => _.GeneratedSources)
            .Where(_ => _.HintName == ProjectReferenceModulesGenerator.HintName)
            .Select(_ => _.SourceText.ToString())
            .SingleOrDefault();

        return new(
            generated,
            [.. generatorDiagnostics, .. output.GetDiagnostics().Where(_ => _.Severity == DiagnosticSeverity.Error)],
            output);
    }

    /// <summary>
    /// Creates a generator driver that tracks its incremental steps.
    /// </summary>
    /// <param name="projectReferences">The value of the <c>CratisArcProjectReferences</c> build property, or null when it is not reported.</param>
    /// <param name="parseOptions">The <see cref="CSharpParseOptions"/> of the compilation.</param>
    /// <returns>The <see cref="GeneratorDriver"/>.</returns>
    public static GeneratorDriver CreateDriver(string? projectReferences, CSharpParseOptions parseOptions)
    {
        var globalOptions = projectReferences is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string> { [ProjectReferenceModulesGenerator.ProjectReferencesProperty] = projectReferences };

        return CSharpGeneratorDriver.Create(
            [new ProjectReferenceModulesGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            optionsProvider: new GlobalAnalyzerConfigOptionsProvider(globalOptions),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    /// <summary>
    /// Creates the compilation of an executable or library.
    /// </summary>
    /// <param name="outputKind">The <see cref="OutputKind"/> of the compilation.</param>
    /// <param name="parseOptions">The <see cref="CSharpParseOptions"/> of the compilation.</param>
    /// <param name="source">The source of the compilation.</param>
    /// <param name="references">The references to the project libraries.</param>
    /// <returns>The <see cref="CSharpCompilation"/>.</returns>
    public static CSharpCompilation CreateCompilation(OutputKind outputKind, CSharpParseOptions parseOptions, string source, IEnumerable<MetadataReference> references) =>
        CSharpCompilation.Create(
            "App",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            PlatformReferences.Concat(references),
            new CSharpCompilationOptions(outputKind));

    /// <summary>
    /// Emits a compilation.
    /// </summary>
    /// <param name="compilation">The <see cref="Compilation"/> to emit.</param>
    /// <returns>The emitted image.</returns>
    /// <exception cref="InvalidOperationException">The compilation does not emit.</exception>
    public static ImmutableArray<byte> Emit(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        return result.Success
            ? [.. stream.ToArray()]
            : throw new InvalidOperationException($"Compilation does not emit: {string.Join(Environment.NewLine, result.Diagnostics)}");
    }
}
