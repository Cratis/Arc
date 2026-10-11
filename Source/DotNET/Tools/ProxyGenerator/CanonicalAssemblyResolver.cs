// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Chooses one physical image per full identity for both eager and on-demand metadata loading.
/// </summary>
/// <param name="assemblyPaths">Assembly paths ordered by preference, with application images first.</param>
internal sealed class CanonicalAssemblyResolver(IEnumerable<string> assemblyPaths) : MetadataAssemblyResolver
{
    readonly Dictionary<string, string> _paths = CanonicalPaths(assemblyPaths);
    PathAssemblyResolver? _fallback;

    /// <inheritdoc/>
    public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
    {
        var loaded = context.GetAssemblies().FirstOrDefault(_ => string.Equals(_.FullName, assemblyName.FullName, StringComparison.OrdinalIgnoreCase));
        if (loaded is not null)
        {
            return loaded;
        }

        if (_paths.TryGetValue(assemblyName.FullName, out var path))
        {
            return context.LoadFromAssemblyPath(path);
        }

        _fallback ??= new PathAssemblyResolver(_paths.Values);
        return _fallback.Resolve(context, assemblyName);
    }

    /// <summary>
    /// Loads the same canonical image the resolver would choose for this path's identity.
    /// </summary>
    /// <param name="context">The metadata context.</param>
    /// <param name="path">The requested application or package path.</param>
    /// <returns>The canonical assembly.</returns>
    internal Assembly Load(MetadataLoadContext context, string path) => Resolve(context, AssemblyName.GetAssemblyName(path))!;

    static Dictionary<string, string> CanonicalPaths(IEnumerable<string> assemblyPaths)
    {
        var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in assemblyPaths)
        {
            var normalized = Path.GetFullPath(path);
            paths.TryAdd(AssemblyName.GetAssemblyName(normalized).FullName, normalized);
        }

        return paths;
    }
}
