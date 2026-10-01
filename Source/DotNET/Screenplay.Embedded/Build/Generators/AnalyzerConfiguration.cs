// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// The configuration the generators of a project are run with.
/// </summary>
/// <remarks>
/// The build writes the properties a generator reads into the generated configuration files it passes to the C#
/// compiler, and those same files are read here with Roslyn's own reader. Hand-building the options instead would
/// mean guessing at precedence between global and per-file sections, and generators configured by a guess are not
/// the generators the compiler is about to run.
/// </remarks>
public sealed class AnalyzerConfiguration : AnalyzerConfigOptionsProvider
{
    readonly ConcurrentDictionary<string, AnalyzerConfigOptions> _resolved = new(StringComparer.Ordinal);
    readonly AnalyzerConfigSet _set;

    AnalyzerConfiguration(AnalyzerConfigSet set)
    {
        _set = set;
        GlobalOptions = new AnalyzerConfigurationValues(set.GlobalConfigOptions.AnalyzerOptions);
    }

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GlobalOptions { get; }

    /// <summary>
    /// Reads the configuration of a project.
    /// </summary>
    /// <param name="paths">The full path of every configuration file the compiler is given.</param>
    /// <returns>The <see cref="AnalyzerConfigOptionsProvider"/> the driver is run with.</returns>
    /// <exception cref="AnalyzerConfigurationIsInvalid">Thrown when the files do not read as one configuration.</exception>
    public static AnalyzerConfigOptionsProvider From(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return UnconfiguredAnalyzers.Instance;
        }

        var configs = paths
            .Select(_ => AnalyzerConfig.Parse(SourceText.From(File.ReadAllText(_)), _))
            .ToImmutableArray();

        var set = AnalyzerConfigSet.Create(configs, out var diagnostics);
        var errors = diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Count > 0)
        {
            throw new AnalyzerConfigurationIsInvalid(errors);
        }

        return new AnalyzerConfiguration(set);
    }

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => For(tree.FilePath);

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => For(textFile.Path);

    /// <summary>
    /// Gets the configuration of a file.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The <see cref="AnalyzerConfigOptions"/>.</returns>
    AnalyzerConfigOptions For(string path) =>
        string.IsNullOrEmpty(path)
            ? GlobalOptions
            : _resolved.GetOrAdd(
                path,
                static (file, set) => new AnalyzerConfigurationValues(set.GetOptionsForSourcePath(file).AnalyzerOptions),
                _set);
}
