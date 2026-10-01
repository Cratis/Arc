// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents one embedded Screenplay document as the catalog describes it.
/// </summary>
/// <param name="Id">The identifier of the document, which is the namespace it describes.</param>
/// <param name="Title">The name the document is presented under.</param>
/// <param name="Namespace">The full namespace the document describes, exactly as the source declares it.</param>
/// <param name="Kind">What part of the application the document describes.</param>
/// <param name="ParentId">The identifier of the document this one sits beneath, null for the assembly document.</param>
/// <param name="ResourceName">The logical name of the manifest resource holding the <c>.play</c> text.</param>
/// <remarks>
/// Everything a navigator needs is here, so a reader builds the tree without loading a single document. The
/// identifier is the namespace rather than a number, which keeps it stable across builds and readable in a URL.
/// </remarks>
public record EmbeddedDocument(
    string Id,
    string Title,
    string Namespace,
    EmbeddedDocumentKind Kind,
    string? ParentId,
    string ResourceName);
