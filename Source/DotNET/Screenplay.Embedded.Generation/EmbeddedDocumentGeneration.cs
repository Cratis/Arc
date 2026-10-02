// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

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
    /// Gets every resource the generation is embedded as - the catalog and each document - by the logical name an
    /// assembly embeds it under.
    /// </summary>
    public IReadOnlyDictionary<string, string> Resources =>
        Documents
            .Select(_ => KeyValuePair.Create(_.Document.ResourceName, _.Source))
            .Append(KeyValuePair.Create(EmbeddedResourceNames.Catalog, Catalog.Serialize()))
            .ToDictionary(StringComparer.Ordinal);

    /// <summary>
    /// Gets a value indicating whether every document was generated without anything being reported as an error.
    /// </summary>
    public bool IsSuccess => !Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error);

    /// <summary>
    /// Gets the generated documents as resources a host can serve without embedding them anywhere.
    /// </summary>
    /// <param name="projectName">The name of the project the documents describe - the name of its assembly.</param>
    /// <returns>The <see cref="InMemoryEventModelResources"/> holding the catalog and every document.</returns>
    /// <remarks>
    /// The resources are read by <see cref="EventModelCatalog"/> exactly as the manifest resources of an assembly
    /// built with the embedded Screenplay package are, so the explorer serves generated documents and embedded
    /// ones the same way.
    /// </remarks>
    public InMemoryEventModelResources ToResources(string projectName) => new(projectName, Resources);
}
