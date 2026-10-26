// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Arc.Slices;

/// <summary>
/// Resolves and caches the <see cref="SliceLocation"/> of types.
/// </summary>
internal static class SliceLocationResolver
{
    static readonly ConcurrentDictionary<Type, SliceLocation> _locations = new();

    /// <summary>
    /// Gets the slice location of a type, resolving it the first time.
    /// </summary>
    /// <param name="type">The type to get for.</param>
    /// <returns>The <see cref="SliceLocation"/> of the type.</returns>
    internal static SliceLocation For(Type type) => _locations.GetOrAdd(type, Resolve);

    /// <summary>
    /// Resolves the slice location of a type from the namespace settings of its assembly, falling back to the
    /// entry assembly when its own assembly declares none.
    /// </summary>
    /// <param name="type">The type to resolve for.</param>
    /// <returns>The resolved <see cref="SliceLocation"/>.</returns>
    internal static SliceLocation Resolve(Type type)
    {
        var settings = SettingsFor(type.Assembly) ?? SettingsFor(Assembly.GetEntryAssembly()) ?? ([], 0);
        return Resolve(type.Namespace ?? string.Empty, settings.Roots, settings.SegmentsToSkip);
    }

    /// <summary>
    /// Resolves a slice location from a namespace.
    /// </summary>
    /// <param name="ns">The namespace to resolve.</param>
    /// <param name="roots">The namespace roots.</param>
    /// <param name="segmentsToSkip">The number of leading segments to skip when no root matches.</param>
    /// <returns>The resolved <see cref="SliceLocation"/>.</returns>
    internal static SliceLocation Resolve(string ns, IEnumerable<string> roots, int segmentsToSkip)
    {
        var (module, feature, slice) = SliceNamespace.ToParts(SliceNamespace.Resolve(ns, roots, segmentsToSkip));
        return new(module, feature, slice);
    }

    static (string[] Roots, int SegmentsToSkip)? SettingsFor(Assembly? assembly)
    {
        if (assembly is null)
        {
            return null;
        }

        var roots = assembly.GetCustomAttributes<SliceNamespaceRootAttribute>().Select(_ => _.Namespace).ToArray();
        var segmentsToSkip = assembly.GetCustomAttribute<SliceNamespaceSegmentsToSkipAttribute>();
        if (roots.Length == 0 && segmentsToSkip is null)
        {
            return null;
        }

        return (roots, segmentsToSkip?.Count ?? 0);
    }
}
