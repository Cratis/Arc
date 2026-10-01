// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

/// <summary>
/// Represents a single Screenplay document embedded in an assembly.
/// </summary>
/// <param name="Id">The identifier of the document - the dotted namespace the generator emitted it for.</param>
/// <param name="Title">The title to show for the document.</param>
/// <param name="Namespace">The namespace the document covers.</param>
/// <param name="Kind">The kind of node the document describes.</param>
/// <param name="ParentId">The identifier of the document holding this one, or null when it is a root.</param>
/// <param name="ResourceName">The manifest resource name the <c>.play</c> source is embedded under.</param>
public record EventModelDocument(
    string Id,
    string Title,
    string Namespace,
    EventModelDocumentKind Kind,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? ParentId,
    string ResourceName);
