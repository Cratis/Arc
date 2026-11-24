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
        var location = SliceLocation.For(part);
        return For(location.Module.Value, location.Feature.Value, location.Slice.Value, part.FullName ?? part.Name);
    }

    /// <summary>
    /// Gets the path of a part from where it lives and what it is called.
    /// </summary>
    /// <param name="module">The module of its slice; empty when it has none.</param>
    /// <param name="feature">The feature of its slice; empty when it has none.</param>
    /// <param name="slice">Its slice; empty when it has none.</param>
    /// <param name="name">The full name of the part.</param>
    /// <returns>The part path.</returns>
    public static string For(string module, string feature, string slice, string name)
    {
        var location = string.Join('/', new[] { module, feature, slice }.Where(_ => _.Length > 0));
        return location.Length == 0 ? name : $"{location}/{name}";
    }

    /// <summary>
    /// Determines whether a path covers a part, given its path and full name.
    /// </summary>
    /// <param name="path">The suspended module, feature, slice or part path.</param>
    /// <param name="partPath">The path of the part.</param>
    /// <param name="name">The full name of the part.</param>
    /// <returns>True when the part is the path or is below it.</returns>
    public static bool Covers(string path, string partPath, string name) =>
        partPath == path || partPath.StartsWith(path + "/", StringComparison.Ordinal) || name == path;

    /// <summary>
    /// Determines whether a path covers a part.
    /// </summary>
    /// <param name="path">The suspended module, feature, slice or part path.</param>
    /// <param name="part">The part.</param>
    /// <returns>True when the part is the path or is below it.</returns>
    public static bool Covers(string path, Type part)
    {
        return Covers(path, For(part), part.FullName ?? part.Name);
    }
}
