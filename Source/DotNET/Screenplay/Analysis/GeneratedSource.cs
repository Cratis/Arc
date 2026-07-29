// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Recognizes source files that are generator output rather than source a developer wrote.
/// </summary>
/// <remarks>
/// A compilation is handed the files a build emits to disk alongside the files a developer authored. A source
/// generator writing partial members into a slice's namespace contributes symbols to the slice but sits under
/// <c>obj/</c>, so it says nothing about where the slice's source - and therefore its screens - actually live.
/// Attributing a slice by such a path spreads it across folders it was never spread over.
/// </remarks>
public static class GeneratedSource
{
    /// <summary>
    /// Determines whether a path names generator output rather than authored source.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True when the path names generator output.</returns>
    public static bool Is(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');

        return HasOutputSegment(normalized, "obj") ||
            HasOutputSegment(normalized, "bin") ||
            normalized.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether a normalized path carries a build-output directory as one of its segments.
    /// </summary>
    /// <param name="normalized">The path, already normalized onto forward slashes.</param>
    /// <param name="segment">The output segment to look for.</param>
    /// <returns>True when the segment appears as a whole path segment.</returns>
    static bool HasOutputSegment(string normalized, string segment) =>
        normalized.Contains($"/{segment}/", StringComparison.OrdinalIgnoreCase) ||
        normalized.StartsWith($"{segment}/", StringComparison.OrdinalIgnoreCase);
}
