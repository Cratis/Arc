// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Slices;

namespace Cratis.Arc.Availability.Suspensions;

/// <summary>
/// The paths that name the modules, features, slices and parts an operator can suspend.
/// </summary>
/// <remarks>
/// A path is the slice location joined with <c>/</c> (<c>Module/Feature/Slice</c>, leaving out what the application does not
/// have) and, for a part, followed by the full name of its type. A path covers itself and everything below it, so
/// suspending a feature suspends every slice and part under it.
/// </remarks>
public static class PartPaths
{
    /// <summary>
    /// Gets the path of a part.
    /// </summary>
    /// <param name="part">The command type, or the type declaring the query.</param>
    /// <returns>The part path.</returns>
    public static string For(Type part)
    {
        var location = SliceLocation.For(part).ToString();
        var name = part.FullName ?? part.Name;
        return location.Length == 0 ? name : $"{location}/{name}";
    }

    /// <summary>
    /// Determines whether a path covers a part.
    /// </summary>
    /// <param name="path">The suspended module, feature, slice or part path.</param>
    /// <param name="part">The part.</param>
    /// <returns>True when the part is the path or is below it.</returns>
    public static bool Covers(string path, Type part)
    {
        var partPath = For(part);
        return partPath == path || partPath.StartsWith(path + "/", StringComparison.Ordinal) || part.FullName == path;
    }
}
