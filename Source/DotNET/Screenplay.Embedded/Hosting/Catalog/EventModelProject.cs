// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents the documents a single assembly contributes to the explorer.
/// </summary>
/// <param name="Id">The identifier of the project - the name of the assembly holding the documents.</param>
/// <param name="Name">The name to show for the project.</param>
/// <param name="Documents">The documents the assembly embeds.</param>
public record EventModelProject(string Id, string Name, IReadOnlyList<EventModelDocument> Documents);
