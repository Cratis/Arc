// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Resolves local proxy identifiers through their import sites.
/// </summary>
/// <param name="namespace">The screen's slice.</param>
/// <param name="path">The importing file.</param>
/// <param name="components">The local import bindings.</param>
/// <param name="queries">The local queries for an isolated reader.</param>
/// <param name="commands">The local commands for an isolated reader.</param>
/// <param name="elsewhere">The complete catalog, when reading an application.</param>
public class ScreenProxyReferences(
    string @namespace,
    string path,
    KnownComponents components,
    IReadOnlyCollection<QueryModel> queries,
    IReadOnlyCollection<CommandModel> commands,
    CrossSliceQueries? elsewhere = null)
{
    /// <summary>
    /// Resolves a command identifier.
    /// </summary>
    /// <param name="local">The local identifier.</param>
    /// <returns>The command reference, or null.</returns>
    public string? Command(string local) => Import(local) is { } import
        ? elsewhere is not null ? elsewhere.Command(@namespace, path, import) : commands.FirstOrDefault(_ => _.Name == import.Name)?.Name
        : null;

    /// <summary>
    /// Resolves a query identifier.
    /// </summary>
    /// <param name="local">The local identifier.</param>
    /// <returns>The query binding, or null.</returns>
    public ScreenDataModel? Query(string local)
    {
        if (Import(local) is not { } import)
        {
            return null;
        }

        if (elsewhere is not null)
        {
            return elsewhere.Query(@namespace, path, import);
        }

        return queries.FirstOrDefault(_ => _.Name == import.Name) is { } query ? new(query.Name, query.ReturnType, query.By?.Name) : null;
    }

    ScreenImport? Import(string local) => components.Imports.TryGetValue(local, out var binding) && binding.IsRelative
        ? new(binding.Name, binding.Module) : null;
}
