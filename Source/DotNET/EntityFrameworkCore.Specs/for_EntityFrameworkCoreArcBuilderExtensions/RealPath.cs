// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkCoreArcBuilderExtensions;

/// <summary>
/// Resolves a path with every symbolic link along it followed, so paths can be compared on systems where the
/// temporary directory sits behind a link (macOS: <c>/var</c> → <c>/private/var</c>).
/// </summary>
public static class RealPath
{
    /// <summary>
    /// Gets the path with all symbolic links in it resolved.
    /// </summary>
    /// <param name="path">The path to resolve.</param>
    /// <returns>The resolved, absolute path.</returns>
    public static string Of(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)!;
        var resolved = root;
        foreach (var segment in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, segment);
            var target = new DirectoryInfo(resolved).ResolveLinkTarget(returnFinalTarget: true);
            if (target is not null)
            {
                resolved = target.FullName;
            }
        }

        return resolved;
    }
}
