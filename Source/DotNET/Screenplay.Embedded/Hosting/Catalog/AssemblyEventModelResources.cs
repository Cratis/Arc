// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents an implementation of <see cref="IEventModelResources"/> reading the manifest resources of an assembly.
/// </summary>
/// <param name="assembly">The assembly to read resources from.</param>
public sealed class AssemblyEventModelResources(Assembly assembly) : IEventModelResources
{
    /// <summary>
    /// Gets the assembly the resources are read from.
    /// </summary>
    public Assembly Assembly { get; } = assembly ?? throw new ArgumentNullException(nameof(assembly));

    /// <inheritdoc/>
    public string Name => Assembly.GetName().Name ?? Assembly.FullName ?? Assembly.ToString();

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Names => Assembly.GetManifestResourceNames();

    /// <inheritdoc/>
    public Stream? Open(string name) => Assembly.GetManifestResourceStream(name);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AssemblyEventModelResources other && other.Assembly == Assembly;

    /// <inheritdoc/>
    public override int GetHashCode() => Assembly.GetHashCode();
}
