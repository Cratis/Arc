// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Works out which file a relative module specifier names.
/// </summary>
/// <remarks>
/// A module specifier is a path written from the file doing the importing, and the folder it lands in is what says
/// which slice the imported name belongs to. Resolving it is therefore the only way a name shared by twenty slices can
/// be tied to one of them. Nothing here touches a disk - the extension is never appended and no file is looked for,
/// because a slice is recognized by its folder rather than by the file within it.
/// </remarks>
public static class ModulePaths
{
    /// <summary>
    /// Resolves the path a module specifier names.
    /// </summary>
    /// <param name="directory">The directory of the file writing the import.</param>
    /// <param name="module">The module specifier.</param>
    /// <returns>The path without an extension, or <see langword="null"/> when the specifier names nothing within.</returns>
    /// <remarks>
    /// A specifier climbing past the root of the paths a compilation was built from names something outside the
    /// application, which no slice of it can be.
    /// </remarks>
    public static string? Resolve(string directory, string module)
    {
        var normalized = directory.Replace('\\', '/');
        var rooted = normalized.StartsWith('/');
        var segments = new List<string>(normalized.Split('/', StringSplitOptions.RemoveEmptyEntries));

        foreach (var segment in module.Replace('\\', '/').Split('/'))
        {
            if (segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal))
            {
                continue;
            }

            if (!string.Equals(segment, "..", StringComparison.Ordinal))
            {
                segments.Add(segment);
                continue;
            }

            if (segments.Count == 0)
            {
                return null;
            }

            segments.RemoveAt(segments.Count - 1);
        }

        return segments.Count == 0 ? null : (rooted ? "/" : string.Empty) + string.Join('/', segments);
    }
}
