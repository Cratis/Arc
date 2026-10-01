// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Runs the source generators of a project over the compilation the documents are generated from.
/// </summary>
/// <remarks>
/// A modern Arc application declares a large part of itself through generators - commands, queries and the types
/// they are bound to. Reading only the authored source would describe an application with those parts missing, and
/// the result would not be a build failure but a document quietly short of what the application really does.
/// <para>
/// This runs over the build's own compilation only. The C# compiler builds its own compilation afterwards and runs
/// the same generators over it, so nothing is written to disk here and no generated file is handed back to the
/// build - two copies of generated source in one compilation is the one outcome worse than none.
/// </para>
/// </remarks>
public static class CompilationGenerators
{
    /// <summary>
    /// Runs the generators.
    /// </summary>
    /// <param name="compilation">The compilation to run them over.</param>
    /// <param name="analyzerPaths">The full path of every analyzer the project compiles with.</param>
    /// <param name="additionalFilePaths">The full path of every additional file the compiler is given.</param>
    /// <param name="analyzerConfigPaths">The full path of every analyzer configuration file the compiler is given.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for cancelling the run.</param>
    /// <returns>The <see cref="CompilationGeneratorResult"/>.</returns>
    /// <exception cref="CompilationIsNotCSharp">Thrown when the compilation is not a C# one.</exception>
    /// <exception cref="AnalyzerNotFound">Thrown when an analyzer the build stated is not on disk.</exception>
    /// <exception cref="AdditionalFileNotFound">Thrown when an additional file the build stated is not on disk.</exception>
    /// <exception cref="AnalyzerConfigurationNotFound">Thrown when a configuration file the build stated is not on disk.</exception>
    /// <exception cref="GeneratorsCouldNotBeLoaded">Thrown when an analyzer could not be read for its generators.</exception>
    /// <exception cref="GeneratorFailed">Thrown when a generator crashed while running.</exception>
    public static CompilationGeneratorResult Run(
        Compilation compilation,
        IEnumerable<string> analyzerPaths,
        IEnumerable<string> additionalFilePaths,
        IEnumerable<string> analyzerConfigPaths,
        CancellationToken cancellationToken = default)
    {
        if (compilation is not CSharpCompilation)
        {
            throw new CompilationIsNotCSharp(compilation);
        }

        var analyzers = Existing(analyzerPaths, _ => new AnalyzerNotFound(_));
        var loader = new AnalyzerAssemblies();
        var generators = GeneratorPlugins.Load(analyzers, loader);
        var additional = Existing(additionalFilePaths, _ => new AdditionalFileNotFound(_));
        var configuration = AnalyzerConfiguration.From(Existing(analyzerConfigPaths, _ => new AnalyzerConfigurationNotFound(_)));

        if (generators.Length == 0)
        {
            return new(compilation, []);
        }

        var driver = CSharpGeneratorDriver.Create(
            generators,
            additional.Select(_ => (AdditionalText)new AdditionalSourceFile(_)),
            ParseOptionsOf(compilation),
            configuration);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out var diagnostics, cancellationToken);

        GeneratorLifetimes.Retain(generated, loader);
        foreach (var diagnostic in diagnostics)
        {
            GeneratorLifetimes.Retain(diagnostic, loader);
        }

        var crashes = diagnostics.Where(GeneratorFailed.IsCrash).ToList();

        if (crashes.Count > 0)
        {
            throw new GeneratorFailed(crashes);
        }

        return new(generated, [.. diagnostics]);
    }

    /// <summary>
    /// Settles the paths the build stated, in an order that does not depend on how the build listed them.
    /// </summary>
    /// <param name="paths">The paths as the build states them.</param>
    /// <param name="missing">Builds the exception thrown for a path that is not on disk.</param>
    /// <returns>The full path of every file, deduplicated and ordered.</returns>
    /// <remarks>
    /// A missing file is never skipped. Generators are order-sensitive in what they are handed, so the same
    /// project has to produce the same list every time it is built.
    /// </remarks>
    static List<string> Existing(IEnumerable<string> paths, Func<string, Exception> missing)
    {
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var settled = paths
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Select(Path.GetFullPath)
            .Distinct(comparer)
            .Order(StringComparer.Ordinal)
            .ToList();

        foreach (var path in settled)
        {
            if (!File.Exists(path))
            {
                throw missing(path);
            }
        }

        return settled;
    }

    /// <summary>
    /// Gets the parse options the generated source is parsed with.
    /// </summary>
    /// <param name="compilation">The compilation being generated over.</param>
    /// <returns>The <see cref="CSharpParseOptions"/>.</returns>
    /// <remarks>
    /// Generated source is parsed with the options the project itself is parsed with, so a generator emitting
    /// source that needs the language version of the project gets the one the project really states.
    /// </remarks>
    static CSharpParseOptions ParseOptionsOf(Compilation compilation) =>
        compilation.SyntaxTrees.Select(_ => _.Options).OfType<CSharpParseOptions>().FirstOrDefault() ??
        CSharpParseOptions.Default;
}
