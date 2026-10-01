// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.Build.Generators;

/// <summary>
/// The configuration values that apply to one file, or to the compilation as a whole.
/// </summary>
/// <param name="values">The values Roslyn resolved for it.</param>
/// <remarks>
/// The dictionary comes out of Roslyn's own configuration reader, so the keys are already the ones a generator
/// asks for - <c>build_property.RootNamespace</c> and the like - compared the way it compares them.
/// </remarks>
public sealed class AnalyzerConfigurationValues(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
{
    /// <summary>
    /// Nothing is configured.
    /// </summary>
    public static readonly AnalyzerConfigurationValues None = new([]);

    /// <inheritdoc/>
    public override IEnumerable<string> Keys => values.Keys;

    /// <inheritdoc/>
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
}
