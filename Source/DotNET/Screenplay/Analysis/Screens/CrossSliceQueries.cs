// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reports the queries a screen reads through that a different slice of the application declares.
/// </summary>
/// <remarks>
/// A screen binds to what a slice declares, and an import naming something the slice does not declare says nothing -
/// which is true of a component, a command and a package, and was taken to be true of everything. It is not true of a
/// query another slice declares: a screen aggregating several read models is what Event Modeling screens routinely
/// do, and dropping those left the document describing screens as reading almost nothing.
/// <para>
/// It still cannot be written down. A <c>data</c> directive names a query by the bare name its slice declares it
/// under, and a real application declares <c>All</c> once per read model, so a name reaching across slices would say
/// which query only by accident (Cratis/Screenplay#28). Where the import was written does say which one, and that is
/// what is reported - which turns a silent drop into something a reader can act on today and a binding the moment a
/// reference can carry the slice.
/// </para>
/// </remarks>
public class CrossSliceQueries
{
    readonly Dictionary<string, string> _owners = new(StringComparer.Ordinal);
    readonly Dictionary<string, HashSet<string>> _declared = new(StringComparer.Ordinal);
    readonly List<ScreenImportSite> _sites = [];

    /// <summary>
    /// Declares the queries a slice holds and the directories its source lives in.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="directories">The directories the source of the slice lives in.</param>
    /// <param name="queries">The queries the slice declares.</param>
    /// <remarks>
    /// A directory claimed by a second slice keeps the first, which is the same answer given wherever a folder holds
    /// the source of more than one slice, and is already reported as the ambiguity it is.
    /// </remarks>
    public void Declare(string @namespace, IReadOnlyList<string> directories, IReadOnlyCollection<QueryModel> queries)
    {
        _declared[@namespace] = [.. queries.Select(_ => _.Name)];

        foreach (var directory in directories)
        {
            _owners.TryAdd(directory, @namespace);
        }
    }

    /// <summary>
    /// Records every name a screen imports from a module sitting alongside it.
    /// </summary>
    /// <param name="namespace">The namespace of the slice the screen belongs to.</param>
    /// <param name="screen">The name of the screen.</param>
    /// <param name="path">The path of the file realizing the screen.</param>
    /// <param name="imports">The names it imported.</param>
    /// <remarks>
    /// Every import is handed over rather than only the ones the slice declares no query under, because the name a
    /// slice declares a query under is the same word twenty other slices declare one under - so a screen importing
    /// <c>All</c> from another slice would be filtered out by the very name that makes it worth reporting.
    /// </remarks>
    public void Record(string @namespace, string screen, string path, IEnumerable<ScreenImport> imports) =>
        _sites.AddRange(imports.Select(_ => new ScreenImportSite(@namespace, screen, ScreenFiles.DirectoryOf(path), _)));

    /// <summary>
    /// Reports every import that named a query of another slice.
    /// </summary>
    /// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> to report to.</param>
    /// <remarks>
    /// Reports are ordered by the slice, the screen and the query rather than by the order the screens happened to be
    /// read, so the same source always reports the same way.
    /// </remarks>
    public void Report(ScreenplayDiagnostics diagnostics)
    {
        var ordered = _sites
            .OrderBy(_ => _.Namespace, StringComparer.Ordinal)
            .ThenBy(_ => _.Screen, StringComparer.Ordinal)
            .ThenBy(_ => _.Import.Name, StringComparer.Ordinal)
            .ThenBy(_ => _.Import.Module, StringComparer.Ordinal);

        foreach (var site in ordered)
        {
            if (DeclaringSlice(site) is not { } owner)
            {
                continue;
            }

            diagnostics.Warning(
                ScreenplayDiagnosticCodes.CrossSliceQueryBinding,
                $"The screen '{site.Screen}' reads through the query '{site.Import.Name}' that '{owner}' declares, and a binding names a query by the bare name the screen's own slice declares it under, so it was left out",
                site.Namespace);
        }
    }

    /// <summary>
    /// Gets the slice an import really names a query of, when it is not the one that wrote it.
    /// </summary>
    /// <param name="site">The import to resolve.</param>
    /// <returns>The namespace of the slice, or <see langword="null"/> when the import names no query of another one.</returns>
    string? DeclaringSlice(ScreenImportSite site)
    {
        if (ModulePaths.Resolve(site.Directory, site.Import.Module) is not { } resolved ||
            !_owners.TryGetValue(ScreenFiles.DirectoryOf(resolved), out var owner) ||
            string.Equals(owner, site.Namespace, StringComparison.Ordinal))
        {
            return null;
        }

        return _declared.TryGetValue(owner, out var queries) && queries.Contains(site.Import.Name) ? owner : null;
    }
}
