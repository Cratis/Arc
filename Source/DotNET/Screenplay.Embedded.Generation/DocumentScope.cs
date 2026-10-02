// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents one document to generate - what of the application it holds, and how that part is named in it.
/// </summary>
/// <param name="Namespace">The full namespace the document describes, exactly as the source declares it.</param>
/// <param name="Title">The name the document is presented under.</param>
/// <param name="Kind">What part of the application the document describes.</param>
/// <param name="ParentId">The identifier of the document this one sits beneath, null for the assembly document.</param>
/// <param name="ModuleName">The name of the module the document declares its features within.</param>
/// <param name="SegmentsToSkip">The number of leading namespace segments the document's features are resolved after.</param>
/// <remarks>
/// Screenplay has no document without a module, so a scoped document declares the module its part of the
/// application already belongs to rather than inventing one per document. A feature document therefore prints the
/// same module, the same feature and the same slices as the assembly document does, with everything outside the
/// scope left out - which is what makes one document a readable subset of the other rather than a second shape.
/// </remarks>
public record DocumentScope(
    string Namespace,
    string Title,
    EmbeddedDocumentKind Kind,
    string? ParentId,
    string ModuleName,
    int SegmentsToSkip)
{
    /// <summary>
    /// Gets the identifier of the document, which is the namespace it describes.
    /// </summary>
    public string Id => Namespace;

    /// <summary>
    /// Gets the catalog entry describing the document.
    /// </summary>
    public EmbeddedDocument Document => new(Id, Title, Namespace, Kind, ParentId, EmbeddedResourceNames.ForDocument(Id));
}
