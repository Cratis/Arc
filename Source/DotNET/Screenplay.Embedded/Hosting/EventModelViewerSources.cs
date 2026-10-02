// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Screenplay.Embedded.Hosting.Catalog;

namespace Cratis.Arc.Screenplay.Embedded.Hosting;

/// <summary>
/// Holds what one mapped explorer serves.
/// </summary>
/// <remarks>
/// <para>
/// The explorer can be asked for twice on the same application - the automatic mapping the Cratis meta-package
/// performs, and an explicit <c>MapCratisEventModel</c> the host writes itself. Mapping the same routes twice
/// makes every request to them ambiguous, so the second call adds its assemblies here instead of mapping again,
/// and both callers get the documents of the assemblies they named, served once.
/// </para>
/// <para>
/// Assemblies are added while the application is being built, on one thread, and read by request handlers
/// afterwards; the volatile fields are what carries the finished catalog across to them.
/// </para>
/// </remarks>
sealed class EventModelViewerSources
{
    readonly List<Assembly> _assemblies = [];
    volatile EventModelExplorer _explorer = EventModelExplorer.Empty;

    /// <summary>
    /// Gets the explorer answering for everything served.
    /// </summary>
    internal EventModelExplorer Explorer => _explorer;

    /// <summary>
    /// Adds assemblies to what the explorer serves.
    /// </summary>
    /// <param name="assemblies">The assemblies to serve embedded documents from.</param>
    /// <exception cref="MalformedEventModelCatalog">Thrown when an assembly embeds a catalog that cannot be read as written.</exception>
    /// <remarks>
    /// The catalog is read here rather than on the first request, so an assembly whose catalog disagrees with
    /// what it embeds fails while the application is starting.
    /// </remarks>
    internal void Include(IEnumerable<Assembly> assemblies)
    {
        var added = assemblies.Where(assembly => !_assemblies.Contains(assembly)).Distinct().ToList();
        if (added.Count == 0)
        {
            return;
        }

        // The catalog is read before anything is kept, so an assembly whose catalog cannot be read leaves what is
        // already served exactly as it was rather than poisoning every later call with its own failure.
        var candidates = new List<Assembly>(_assemblies);
        candidates.AddRange(added);
        var catalog = EventModelCatalog.For(candidates);

        _assemblies.Clear();
        _assemblies.AddRange(candidates);
        _explorer = new EventModelExplorer(catalog);
    }
}
