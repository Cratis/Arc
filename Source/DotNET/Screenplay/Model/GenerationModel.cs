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
    /// Gets whether the generation strategy requires ESM v21.
    /// </summary>
    public bool RequiresV21 => Kind == "pattern" || Kind == "random" || Kind == "rule";

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

    /// <summary>
    /// Gets the finite generation pattern.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Gets the appended checksum algorithm.
    /// </summary>
    public string? Checksum { get; init; }

    /// <summary>
    /// Gets the exact inclusive integer range.
    /// </summary>
    public string? Range { get; init; }

    /// <summary>
    /// Gets the self-contained rule name.
    /// </summary>
    public string? Rule { get; init; }

    /// <summary>
    /// Gets the primitive-returning C# rule body.
    /// </summary>
    public string? Body { get; init; }
}
