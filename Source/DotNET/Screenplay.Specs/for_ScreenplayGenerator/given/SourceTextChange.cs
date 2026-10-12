// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Describes one exact source-text substitution in a generated-value fixture.
/// </summary>
/// <param name="Original">The source text being replaced.</param>
/// <param name="Replacement">The replacement text.</param>
internal record SourceTextChange(string Original, string Replacement)
{
    /// <summary>
    /// Applies the ordinal text substitution to a fixture.
    /// </summary>
    /// <param name="source">The original fixture source.</param>
    /// <returns>The fixture source after substitution.</returns>
    public string Apply(string source) => source.Replace(Original, Replacement, StringComparison.Ordinal);
}
