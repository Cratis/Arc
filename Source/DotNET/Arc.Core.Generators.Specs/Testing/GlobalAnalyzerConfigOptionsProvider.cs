// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Represents an <see cref="AnalyzerConfigOptionsProvider"/> that supplies only global options, the way MSBuild
/// passes <c>CompilerVisibleProperty</c> values to a generator.
/// </summary>
/// <param name="globalOptions">The global options, by key.</param>
public sealed class GlobalAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
{
    static readonly GlobalAnalyzerConfigOptions _empty = new(new Dictionary<string, string>());

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GlobalOptions { get; } = new GlobalAnalyzerConfigOptions(globalOptions);

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _empty;

    /// <inheritdoc/>
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _empty;
}
