// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// The configuration of a project that states no analyzer configuration files at all.
/// </summary>
/// <remarks>
/// A generator reading a property it was never given has to see what it sees in a project with no configuration -
/// nothing - rather than a provider that throws or one holding values this build made up.
/// </remarks>
public sealed class UnconfiguredAnalyzers : AnalyzerConfigOptionsProvider
{
    /// <summary>
    /// The one instance of <see cref="UnconfiguredAnalyzers"/>.
    /// </summary>
    public static readonly UnconfiguredAnalyzers Instance = new();

    UnconfiguredAnalyzers()
    {
    }

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GlobalOptions => AnalyzerConfigurationValues.None;

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => AnalyzerConfigurationValues.None;

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => AnalyzerConfigurationValues.None;
}
