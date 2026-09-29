// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Arc.Generators.Specs.Testing;

/// <summary>
/// Represents an <see cref="AssemblyLoadContext"/> for in-memory compiled assemblies, resolving everything else,
/// Cratis.Arc.Core included, from the default context.
/// </summary>
public sealed class ProjectLibraryLoadContext() : AssemblyLoadContext(nameof(ProjectLibraryLoadContext))
{
    readonly Dictionary<string, Assembly> _assemblies = new(StringComparer.Ordinal);

    /// <summary>
    /// Loads a <see cref="ProjectLibrary"/> into the context.
    /// </summary>
    /// <param name="library">The <see cref="ProjectLibrary"/> to load.</param>
    /// <returns>The loaded <see cref="Assembly"/>.</returns>
    public Assembly Add(ProjectLibrary library)
    {
        using var stream = new MemoryStream([.. library.Image]);
        var assembly = LoadFromStream(stream);
        _assemblies[library.Name] = assembly;
        return assembly;
    }

    /// <inheritdoc/>
    protected override Assembly? Load(AssemblyName assemblyName) =>
        assemblyName.Name is { } name && _assemblies.TryGetValue(name, out var assembly) ? assembly : null;
}
