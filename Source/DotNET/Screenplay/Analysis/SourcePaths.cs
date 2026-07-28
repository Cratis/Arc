// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Turns the absolute paths a compilation carries into paths a generated document can reference.
/// </summary>
/// <param name="root">The directory every path is written relative to.</param>
/// <remarks>
/// Syntax trees carry whatever path the compiler was handed, which for a real build is absolute and specific to
/// the machine it ran on. A document is meant to be committed and diffed, so paths are written relative to the
/// deepest directory every source file shares.
/// </remarks>
public class SourcePaths(string root)
{
    /// <summary>
    /// Resolves the shared root of every source file in a compilation.
    /// </summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <returns>The <see cref="SourcePaths"/>.</returns>
    public static SourcePaths For(Compilation compilation)
    {
        var directories = compilation.SyntaxTrees
            .Select(_ => _.FilePath)
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Select(Normalize)
            .Select(_ => _[..(_.LastIndexOf('/') + 1)])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        return new(directories.Count == 0 ? string.Empty : CommonPrefix(directories));
    }

    /// <summary>
    /// Rewrites a path so that it is relative to the shared root.
    /// </summary>
    /// <param name="path">The path to rewrite.</param>
    /// <returns>The relative path, or <see langword="null"/> when there is none.</returns>
    public string? Relative(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalized = Normalize(path);

        return root.Length > 0 && normalized.StartsWith(root, StringComparison.Ordinal)
            ? normalized[root.Length..]
            : normalized;
    }

    /// <summary>
    /// Normalizes a path onto forward slashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Finds the longest directory prefix every path shares.
    /// </summary>
    /// <param name="directories">The directories to compare.</param>
    /// <returns>The shared prefix, ending in a separator.</returns>
    static string CommonPrefix(List<string> directories)
    {
        var prefix = directories[0];

        foreach (var directory in directories.Skip(1))
        {
            var length = 0;
            while (length < prefix.Length && length < directory.Length && prefix[length] == directory[length])
            {
                length++;
            }

            prefix = prefix[..length];
        }

        var separator = prefix.LastIndexOf('/');

        return separator < 0 ? string.Empty : prefix[..(separator + 1)];
    }
}
