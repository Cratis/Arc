// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Resolves screen imports against the slice declaring them once every slice has been read.
/// </summary>
public class CrossSliceQueries
{
    readonly Dictionary<string, string> _owners = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<QueryModel>> _queries = new(StringComparer.Ordinal);
    readonly Dictionary<string, HashSet<string>> _commands = new(StringComparer.Ordinal);
    readonly Dictionary<ScreenModel, Func<ScreenModel>> _pending = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Declares the commands, queries and source directories of a slice.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="directories">The source directories.</param>
    /// <param name="queries">The declared queries.</param>
    /// <param name="commands">The declared commands.</param>
    public void Declare(string @namespace, IReadOnlyList<string> directories, IReadOnlyCollection<QueryModel> queries, IReadOnlyCollection<CommandModel> commands)
    {
        if (!_queries.TryGetValue(@namespace, out var declaredQueries))
        {
            _queries[@namespace] = declaredQueries = [];
            _commands[@namespace] = new(StringComparer.Ordinal);
        }

        declaredQueries.AddRange(queries);
        _commands[@namespace].UnionWith(commands.Select(_ => _.Name));
        foreach (var directory in directories)
        {
            _owners.TryAdd(directory, @namespace);
        }
    }

    /// <summary>
    /// Defers reading a screen until every imported declaration is known.
    /// </summary>
    /// <param name="screen">The placeholder retaining the screen's identity.</param>
    /// <param name="read">The reader of its directives.</param>
    public void Defer(ScreenModel screen, Func<ScreenModel> read) => _pending.Add(screen, read);

    /// <summary>
    /// Reads the retained screens against the complete declaration catalog.
    /// </summary>
    /// <param name="slices">The slices joined across projects.</param>
    /// <returns>The slices with their screens resolved.</returns>
    public IReadOnlyList<SliceModel> Apply(IReadOnlyList<SliceModel> slices) =>
        [.. slices.Select(slice => slice with
        {
            Screens = slice.Screens.Select(screen => _pending.TryGetValue(screen, out var read) ? read() : screen).ToList()
        })];

    /// <summary>
    /// Resolves a command by the module the screen imported it from, not by its name alone.
    /// </summary>
    /// <param name="namespace">The screen's slice.</param>
    /// <param name="path">The importing file.</param>
    /// <param name="import">The import.</param>
    /// <returns>The bare local name or an owner-qualified reference, or null when unresolved.</returns>
    public string? Command(string @namespace, string path, ScreenImport import) =>
        Owner(path, import) is { } owner && _commands[owner].Contains(import.Name) ? Reference(@namespace, owner, import.Name) : null;

    /// <summary>
    /// Resolves a query by the module the screen imported it from.
    /// </summary>
    /// <param name="namespace">The screen's slice.</param>
    /// <param name="path">The importing file.</param>
    /// <param name="import">The import.</param>
    /// <returns>The binding and its declared return type, or null when unresolved.</returns>
    public ScreenDataModel? Query(string @namespace, string path, ScreenImport import) =>
        Owner(path, import) is { } owner && _queries[owner].Find(_ => _.Name == import.Name) is { } query
            ? new(Reference(@namespace, owner, query.Name), query.ReturnType, query.By?.Name)
            : null;

    static string Reference(string @namespace, string owner, string name) => owner == @namespace ? name : $"{owner}.{name}";

    string? Owner(string path, ScreenImport import) =>
        ModulePaths.Resolve(ScreenFiles.DirectoryOf(path), import.Module) is { } resolved &&
        _owners.TryGetValue(ScreenFiles.DirectoryOf(resolved), out var owner) ? owner : null;
}
