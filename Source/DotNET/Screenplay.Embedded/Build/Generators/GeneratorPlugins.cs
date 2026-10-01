// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// Reads the source generators out of the analyzers a project compiles with.
/// </summary>
/// <remarks>
/// These are the real analyzer assemblies of the project, read the way the C# compiler reads them, so what runs
/// here is what will run minutes later in the compiler itself - not a stand-in and not a subset.
/// </remarks>
public static class GeneratorPlugins
{
    /// <summary>
    /// Loads the generators of the analyzers.
    /// </summary>
    /// <param name="analyzerPaths">The full path of every analyzer, in the order they are to be run in.</param>
    /// <param name="loader">The loader retained for the compilation's lifetime.</param>
    /// <returns>Every generator the analyzers declare for C#.</returns>
    /// <exception cref="GeneratorsCouldNotBeLoaded">Thrown when an analyzer could not be read for its generators.</exception>
    /// <remarks>
    /// An analyzer that declares no generator is not a failure - plenty of analyzer packages only analyze. An
    /// analyzer that could not be read is, because what it would have generated is unknown by definition.
    /// </remarks>
    public static ImmutableArray<ISourceGenerator> Load(IReadOnlyList<string> analyzerPaths, AnalyzerAssemblies loader)
    {
        if (analyzerPaths.Count == 0)
        {
            return [];
        }

        foreach (var path in analyzerPaths)
        {
            loader.AddDependencyLocation(path);
        }

        var failures = new List<string>();
        var generators = new List<ISourceGenerator>();

        foreach (var path in analyzerPaths)
        {
            var reference = new AnalyzerFileReference(path, loader);
            reference.AnalyzerLoadFailed += (_, failure) =>
            {
                if (failure.ErrorCode != AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers)
                {
                    failures.Add(Describe(path, failure));
                }
            };
            generators.AddRange(reference.GetGenerators(LanguageNames.CSharp));
        }

        if (failures.Count > 0)
        {
            throw new GeneratorsCouldNotBeLoaded(failures);
        }

        return [.. generators];
    }

    /// <summary>
    /// Describes what went wrong reading an analyzer.
    /// </summary>
    /// <param name="path">The path of the analyzer.</param>
    /// <param name="failure">What Roslyn reported about it.</param>
    /// <returns>The description.</returns>
    static string Describe(string path, AnalyzerLoadFailureEventArgs failure)
    {
        var type = string.IsNullOrEmpty(failure.TypeName) ? string.Empty : $" ({failure.TypeName})";
        var reason = failure.Exception is null ? failure.Message : $"{failure.Message}: {failure.Exception.Message}";

        return $"  {path}{type} - {failure.ErrorCode}: {reason}";
    }
}
