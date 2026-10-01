// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents the catalog document the build embeds beside the generated <c>.play</c> resources.
/// </summary>
/// <remarks>
/// Every member is nullable on purpose: the manifest is read exactly as it was written, and
/// <see cref="EventModelCatalog"/> turns anything missing into a <see cref="MalformedEventModelCatalog"/>
/// instead of defaulting it into something that reads as valid.
/// </remarks>
internal class EventModelCatalogManifest
{
    /// <summary>
    /// Gets or sets the documents the assembly embeds.
    /// </summary>
    public IList<EventModelCatalogManifestDocument>? Documents { get; set; }
}
