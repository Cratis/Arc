// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents a proven concept-owned generation strategy.
/// </summary>
/// <param name="Kind">The portable strategy name.</param>
internal sealed record GenerationModel(string Kind)
{
    /// <summary>
    /// Gets the TypeID prefix.
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// Gets the non-default encoded letter case.
    /// </summary>
    public string? Case { get; init; }

    /// <summary>
    /// Gets the explicitly supplied NanoID size.
    /// </summary>
    public int? Size { get; init; }

    /// <summary>
    /// Gets the explicitly supplied NanoID alphabet.
    /// </summary>
    public string? Alphabet { get; init; }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    public int? Count { get; init; }

    /// <summary>
    /// Gets the byte encoding.
    /// </summary>
    public string? Encoding { get; init; }
}
