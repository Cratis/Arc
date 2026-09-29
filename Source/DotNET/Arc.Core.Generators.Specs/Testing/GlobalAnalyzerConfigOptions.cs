// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Represents analyzer config options backed by a fixed set of values.
/// </summary>
/// <param name="values">The values, by key.</param>
public sealed class GlobalAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
{
    /// <inheritdoc/>
    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
}
