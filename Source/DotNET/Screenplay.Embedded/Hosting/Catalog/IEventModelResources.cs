// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Defines a set of named resources a project's Screenplay catalog and documents are read from.
/// </summary>
/// <remarks>
/// An assembly built with the embedded Screenplay package holds its catalog and documents as manifest resources,
/// which is what <see cref="AssemblyEventModelResources"/> reads. A host that generates the same documents itself -
/// a command line tool viewing an application that was built without them - holds them in memory instead, which
/// is what <see cref="InMemoryEventModelResources"/> reads. Both are read by <see cref="EventModelCatalog"/> in
/// exactly the same way, so neither can describe an application differently than the other would.
/// </remarks>
public interface IEventModelResources
{
    /// <summary>
    /// Gets the name of the project the resources belong to - the name of the assembly they describe.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the names of every resource held.
    /// </summary>
    IReadOnlyCollection<string> Names { get; }

    /// <summary>
    /// Opens a resource for reading.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A <see cref="Stream"/> the caller owns, or null when no resource has the name.</returns>
    Stream? Open(string name);
}
