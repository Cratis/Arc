// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Routes;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Resolves screen imports against the slice declaring them once every slice has been read.
/// </summary>
public class CrossSliceQueries
{
    readonly Dictionary<string, List<string>> _owners = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<QueryModel>> _queries = new(StringComparer.Ordinal);
    readonly Dictionary<string, HashSet<string>> _commands = new(StringComparer.Ordinal);
    readonly Dictionary<ScreenModel, Func<ScreenModel>> _pending = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<ScreenModel, RouteScreen> _screens = new(ReferenceEqualityComparer.Instance);
    readonly RouteCatalog _routes = new();

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
            if (!_owners.TryGetValue(directory, out var claimants))
            {
                _owners[directory] = claimants = [];
            }

            claimants.Add(@namespace);
        }
    }

    /// <summary>
    /// Defers reading a screen until every imported declaration is known.
    /// </summary>
    /// <param name="screen">The placeholder retaining the screen's identity.</param>
    /// <param name="read">The reader of its directives.</param>
    public void Defer(ScreenModel screen, Func<ScreenModel> read) => _pending.Add(screen, read);

    /// <summary>
    /// Retains the original file identity of a screen before its path is made relative.
    /// </summary>
    /// <param name="screen">The retained screen placeholder.</param>
    /// <param name="namespace">The declaring slice.</param>
    /// <param name="path">The original source path.</param>
    public void DeclareScreen(ScreenModel screen, string @namespace, string path) => _screens.Add(screen, new(@namespace, screen.Name, path));

    /// <summary>
    /// Reads route tables before deferred screen gestures are resolved.
    /// </summary>
    /// <param name="files">The user interface file source.</param>
    /// <param name="roots">The source roots searched recursively.</param>
    /// <param name="slices">The slices retained after application-wide placement.</param>
    /// <param name="diagnostics">The omission reporter.</param>
    public void ReadRoutes(IUserInterfaceFiles files, IEnumerable<string> roots, IReadOnlyList<SliceModel> slices, ScreenplayDiagnostics diagnostics) =>
        _routes.Read(files, roots, slices.SelectMany(_ => _.Screens).Select(_ => _screens.GetValueOrDefault(_)).OfType<RouteScreen>().ToList(), diagnostics);

    /// <summary>
    /// Resolves a literal navigation gesture against the route table's screen files.
    /// </summary>
    /// <param name="namespace">The navigating screen's slice.</param>
    /// <param name="literal">The literal router destination.</param>
    /// <returns>The destination and parameters, or null when not proven.</returns>
    public ScreenNavigationModel? Navigation(string @namespace, string literal) => _routes.Resolve(@namespace, literal);

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
        Owner(@namespace, path, import, owner => _commands.TryGetValue(owner, out var commands) && commands.Contains(import.Name)) is { } owner ? Reference(@namespace, owner, import.Name) : null;

    /// <summary>
    /// Resolves a query by the module the screen imported it from.
    /// </summary>
    /// <param name="namespace">The screen's slice.</param>
    /// <param name="path">The importing file.</param>
    /// <param name="import">The import.</param>
    /// <returns>The binding and its declared return type, or null when unresolved.</returns>
    public ScreenDataModel? Query(string @namespace, string path, ScreenImport import) =>
        Owner(@namespace, path, import, owner => _queries.TryGetValue(owner, out var queries) && queries.Exists(_ => _.Name == import.Name)) is { } owner && _queries[owner].Find(_ => _.Name == import.Name) is { } query
            ? new(Reference(@namespace, owner, query.Name), query.ReturnType, query.By?.Name)
            : null;

    static string Reference(string @namespace, string owner, string name) => owner == @namespace ? name : $"{owner}.{name}";

    /// <summary>
    /// Finds the slice declaring an import.
    /// </summary>
    /// <param name="namespace">The screen's slice.</param>
    /// <param name="path">The importing file.</param>
    /// <param name="import">The import.</param>
    /// <param name="declares">Whether a slice declares the imported name.</param>
    /// <returns>The declaring slice, or null when no slice is proven to declare it.</returns>
    /// <remarks>
    /// The screen's own slice wins when it claims the imported directory, which keeps a folder holding the source of
    /// several slices bound the way it always was. An unresolvable module path or a directory claimed by no slice
    /// cannot establish ownership, even when the screen's own slice declares the imported name.
    /// </remarks>
    string? Owner(string @namespace, string path, ScreenImport import, Func<string, bool> declares)
    {
        var claimants = ModulePaths.Resolve(ScreenFiles.DirectoryOf(path), import.Module) is { } resolved &&
            _owners.TryGetValue(ScreenFiles.DirectoryOf(resolved), out var owners) ? owners : [];
        var owner = claimants.Contains(@namespace) ? @namespace : claimants.FirstOrDefault();
        return owner is not null && declares(owner) ? owner : null;
    }
}
