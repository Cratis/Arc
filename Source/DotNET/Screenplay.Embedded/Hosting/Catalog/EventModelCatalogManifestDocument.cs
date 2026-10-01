// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents a single entry in the embedded catalog document, exactly as it was written.
/// </summary>
internal class EventModelCatalogManifestDocument
{
    /// <summary>
    /// Gets or sets the identifier of the document - the dotted namespace the generator emitted it for.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the title to show for the document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the namespace the document covers.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets the kind of node the document describes - <c>assembly</c>, <c>module</c> or <c>feature</c>.
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the document holding this one, or null when it is a root.
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the manifest resource name the <c>.play</c> source is embedded under.
    /// </summary>
    public string? ResourceName { get; set; }
}
