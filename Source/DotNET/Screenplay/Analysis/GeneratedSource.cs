// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Recognizes the source a build wrote rather than a person.
/// </summary>
/// <remarks>
/// A compilation loaded from a project carries the output of every source generator that ran, emitted to disk under
/// the intermediate folder. Those files declare real members of real types, so they belong in the model - but they
/// say nothing whatsoever about where the application is written, and letting them answer that question puts a build
/// folder among the folders a slice is said to live in. What follows from that is a slice reported as spread over
/// two folders when it sits in one, and a screen scan widened to a tree no screen was ever written in.
/// </remarks>
public static class GeneratedSource
{
    /// <summary>
    /// The folders a build writes its output to.
    /// </summary>
    public static readonly string[] OutputFolders = ["obj", "bin"];

    /// <summary>
    /// The suffixes a generated file is conventionally named with.
    /// </summary>
    public static readonly string[] Suffixes = [".g.cs", ".g.i.cs", ".generated.cs"];

    /// <summary>
    /// Determines whether a path names source a build wrote.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True when the path names generated source.</returns>
    public static bool Is(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');

        return Array.Exists(Suffixes, _ => normalized.EndsWith(_, StringComparison.OrdinalIgnoreCase)) ||
            normalized
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .SkipLast(1)
                .Any(segment => Array.Exists(OutputFolders, _ => string.Equals(segment, _, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Removes the paths naming source a build wrote, keeping every one of them when none is left otherwise.
    /// </summary>
    /// <param name="paths">The paths to filter.</param>
    /// <returns>The paths a person wrote.</returns>
    /// <remarks>
    /// Falling back to the whole set is what keeps a compilation of nothing but generated source answering at all.
    /// Where a person wrote none of it there is no better answer available, and an empty one would leave every path
    /// in the document written against the machine that generated it.
    /// </remarks>
    public static IReadOnlyList<string> Excluded(IEnumerable<string?> paths)
    {
        var all = paths.Where(_ => !string.IsNullOrWhiteSpace(_)).Select(_ => _!).ToList();
        var written = all.FindAll(_ => !Is(_));

        return written.Count > 0 ? written : all;
    }
}
