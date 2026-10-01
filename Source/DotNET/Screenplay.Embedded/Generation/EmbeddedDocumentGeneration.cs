// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Generation;

/// <summary>
/// Represents everything generating the embedded documents of an assembly produced.
/// </summary>
/// <param name="Documents">Every generated document, parents before the documents beneath them.</param>
/// <param name="Diagnostics">Everything that could not be expressed, reported rather than dropped.</param>
public record EmbeddedDocumentGeneration(
    IReadOnlyList<GeneratedDocument> Documents,
    IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Represents a generation that produced nothing at all.
    /// </summary>
    public static readonly EmbeddedDocumentGeneration None = new([], []);

    /// <summary>
    /// Gets the catalog describing every generated document.
    /// </summary>
    public EmbeddedDocumentCatalog Catalog => new([.. Documents.Select(_ => _.Document)]);

    /// <summary>
    /// Gets a value indicating whether every document was generated without anything being reported as an error.
    /// </summary>
    public bool IsSuccess => !Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error);
}
