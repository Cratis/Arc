// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Text;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents an implementation of <see cref="IEventModelResources"/> holding its resources in memory.
/// </summary>
/// <remarks>
/// The resources are named exactly as an assembly would embed them - the catalog under
/// <c>Cratis.Arc.Screenplay.Embedded.catalog.json</c> and each document under the name the catalog states - so
/// documents generated in memory are read through the same catalog an embedded assembly is.
/// </remarks>
public sealed class InMemoryEventModelResources : IEventModelResources
{
    readonly FrozenDictionary<string, byte[]> _resources;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventModelResources"/> class.
    /// </summary>
    /// <param name="name">The name of the project the resources belong to.</param>
    /// <param name="resources">The text of every resource, by the name it is held under.</param>
    public InMemoryEventModelResources(string name, IReadOnlyDictionary<string, string> resources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resources);

        Name = name;
        _resources = resources.ToFrozenDictionary(_ => _.Key, _ => Encoding.UTF8.GetBytes(_.Value), StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IReadOnlyCollection<string> Names => _resources.Keys;

    /// <inheritdoc/>
    public Stream? Open(string name) =>
        _resources.TryGetValue(name, out var content) ? new MemoryStream(content, writable: false) : null;
}
