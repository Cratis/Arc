// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay;

/// <summary>
/// Represents the options a Screenplay document is generated with.
/// </summary>
public record ScreenplayOptions
{
    /// <summary>
    /// The name used for the domain and module when nothing else can be resolved.
    /// </summary>
    public const string DefaultName = "Application";

    /// <summary>
    /// Gets the name of the domain the generated document belongs to.
    /// </summary>
    /// <remarks>
    /// Defaults to the name of the assembly being analyzed.
    /// </remarks>
    public string? Domain { get; init; }

    /// <summary>
    /// Gets the name of the module every discovered feature is placed within.
    /// </summary>
    /// <remarks>
    /// Defaults to the same value as <see cref="Domain"/>.
    /// </remarks>
    public string? Module { get; init; }

    /// <summary>
    /// Gets the number of leading namespace segments to skip when arranging slices into features.
    /// </summary>
    /// <remarks>
    /// Defaults to zero. Regardless of this value, a leading segment matching the module name is always dropped so
    /// that the module is never repeated as a feature.
    /// </remarks>
    public int? SegmentsToSkip { get; init; }

    /// <summary>
    /// Resolves the options with every value filled in.
    /// </summary>
    /// <param name="fallbackName">The name to use for the domain when none is configured.</param>
    /// <returns>The resolved options.</returns>
    public ScreenplayOptions WithDefaults(string? fallbackName)
    {
        var domain = Coalesce(Domain, Coalesce(fallbackName, DefaultName));

        return this with
        {
            Domain = domain,
            Module = Coalesce(Module, domain),
            SegmentsToSkip = SegmentsToSkip ?? 0
        };
    }

    /// <summary>
    /// Gets the first of two values that carries content.
    /// </summary>
    /// <param name="value">The preferred value.</param>
    /// <param name="fallback">The value to fall back to.</param>
    /// <returns>The resolved value.</returns>
    static string Coalesce(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
