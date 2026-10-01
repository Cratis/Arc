// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Compiles each immutable embedded document once, without sharing a compiler between concurrent parses.
/// </summary>
/// <param name="catalog">The embedded source catalog.</param>
public class CompiledEventModels(EventModelCatalog catalog)
{
    readonly ConcurrentDictionary<(string Project, string Document), Lazy<EventModelView>> _models = new();

    /// <summary>
    /// Gets the compiled view of an embedded document.
    /// </summary>
    /// <param name="projectId">The owning assembly.</param>
    /// <param name="documentId">The document to compile.</param>
    /// <param name="model">The compiled model when the document exists.</param>
    /// <returns>Whether the catalog contains the document.</returns>
    public bool TryGet(string projectId, string documentId, [NotNullWhen(true)] out EventModelView? model)
    {
        model = null;
        if (!catalog.TryGetSource(projectId, documentId, out var source))
        {
            return false;
        }

        model = _models.GetOrAdd(
            (projectId, documentId),
            static (key, text) => new(() => new EventModelParser().Parse(key.Document, key.Document, text)),
            source).Value;
        return true;
    }
}
