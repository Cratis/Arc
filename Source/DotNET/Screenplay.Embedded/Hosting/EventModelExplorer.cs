// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Screenplay.Embedded.Hosting.Board;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// Represents everything the event model explorer answers - the hierarchy of projects and documents, the
/// Screenplay source of a document and the board model it compiles to - independent of how it is served.
/// </summary>
/// <param name="catalog">The <see cref="EventModelCatalog"/> holding the documents to explore.</param>
/// <remarks>
/// <para>
/// The HTTP endpoints an application maps with <c>MapCratisEventModel</c> are a thin translation of this onto
/// routes. A host serving the same viewer some other way - a command line tool viewing an application from the
/// outside, with documents it read from the built assembly or generated in memory - builds an explorer over its
/// own catalog and serves the same answers, so the viewer cannot tell the two apart.
/// </para>
/// <para>
/// Each document is compiled once, on the first request for its model, and the result is kept for the lifetime
/// of the explorer since the source of a document never changes underneath it.
/// </para>
/// </remarks>
public sealed class EventModelExplorer(EventModelCatalog catalog)
{
    readonly CompiledEventModels _models = new(catalog);

    /// <summary>
    /// Gets an explorer with nothing to explore.
    /// </summary>
    public static EventModelExplorer Empty { get; } = new(EventModelCatalog.For(Array.Empty<IEventModelResources>()));

    /// <summary>
    /// Gets the catalog being explored.
    /// </summary>
    public EventModelCatalog Catalog { get; } = catalog ?? throw new ArgumentNullException(nameof(catalog));

    /// <summary>
    /// Gets the hierarchy - every project and the documents it holds.
    /// </summary>
    public IReadOnlyList<EventModelProject> Hierarchy => Catalog.Projects;

    /// <summary>
    /// Tries to get the Screenplay source of a document.
    /// </summary>
    /// <param name="projectId">The identifier of the project holding the document.</param>
    /// <param name="documentId">The identifier of the document.</param>
    /// <param name="source">When this method returns, holds the source of the document.</param>
    /// <returns>True when the document is known, false otherwise.</returns>
    public bool TryGetSource(string projectId, string documentId, out string source) =>
        Catalog.TryGetSource(projectId, documentId, out source);

    /// <summary>
    /// Tries to get the board model a document compiles to.
    /// </summary>
    /// <param name="projectId">The identifier of the project holding the document.</param>
    /// <param name="documentId">The identifier of the document.</param>
    /// <param name="model">When this method returns, holds the compiled model of the document.</param>
    /// <returns>True when the document is known, false otherwise.</returns>
    public bool TryGetModel(string projectId, string documentId, [NotNullWhen(true)] out EventModelView? model) =>
        _models.TryGet(projectId, documentId, out model);
}
