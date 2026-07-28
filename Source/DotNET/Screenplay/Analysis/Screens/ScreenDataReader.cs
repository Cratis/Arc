// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads which of a slice's queries a screen binds, and says plainly what it does not read.
/// </summary>
/// <param name="files">The <see cref="IUserInterfaceFiles"/> the text of a component is asked of.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything not inferred is reported to.</param>
/// <param name="elsewhere">The <see cref="CrossSliceQueries"/> a name matching no query of the slice is held by.</param>
/// <remarks>
/// Arc generates a proxy per query and a component imports it by name, so an import is a name the model can be held
/// against rather than a reading of a user interface. A name matching a query the slice declares is a binding.
/// Nothing about the binding is taken from the component beyond the name - the type and the key come from the query,
/// which is C#.
/// <para>
/// A name matching nothing the slice declares is handed on rather than dropped, because a query another slice
/// declares is a binding too and the only reason it cannot be written down is that a reference has no way to say
/// which slice it belongs to. What that name means depends on every slice the application has, so answering it waits
/// until they have all been read.
/// </para>
/// <para>
/// What a view model beside the component imports counts as what the component imports, because where components are
/// written that way the query is named there and the component names only the view model - see
/// <see cref="ViewModelImports"/> for how far that is followed.
/// </para>
/// <para>
/// Every screen also reports what stays out. The rest of the declarative form is JSX structure, and the cost of
/// guessing it wrong is a document that states something about the application that is not so - which is worse than
/// a document that says less. Saying so per screen is what turns that limit from a silence into an answer.
/// </para>
/// </remarks>
public class ScreenDataReader(IUserInterfaceFiles files, ScreenplayDiagnostics diagnostics, CrossSliceQueries elsewhere)
{
    /// <summary>
    /// Reads the bindings of one screen.
    /// </summary>
    /// <param name="namespace">The namespace of the slice the screen belongs to.</param>
    /// <param name="name">The name of the screen.</param>
    /// <param name="path">The path of the file realizing it, as the compilation spells it.</param>
    /// <param name="queries">The queries the slice declares, under the names it declares them.</param>
    /// <returns>The bindings, ordered by the query they read through.</returns>
    public IEnumerable<ScreenDataModel> Read(
        string @namespace,
        string name,
        string path,
        IReadOnlyCollection<QueryModel> queries)
    {
        var written = ScreenImports.Statements(files.Contents(path));
        var imports = written.Concat(ViewModelImports.Of(path, written, files)).Distinct().ToList();
        var imported = new HashSet<string>(imports.Select(_ => _.Name), StringComparer.Ordinal);

        ReportUninferredStructure(@namespace, name);
        elsewhere.Record(@namespace, name, path, imports);

        return
        [
            .. queries
                .Where(_ => imported.Contains(_.Name))
                .OrderBy(_ => _.Name, StringComparer.Ordinal)
                .Select(_ => new ScreenDataModel(_.Name, _.ReturnType, _.By?.Name))
        ];
    }

    /// <summary>
    /// Reports the part of a screen's body that is never inferred.
    /// </summary>
    /// <param name="namespace">The namespace of the slice the screen belongs to.</param>
    /// <param name="name">The name of the screen.</param>
    void ReportUninferredStructure(string @namespace, string name) =>
        diagnostics.Information(
            ScreenplayDiagnosticCodes.ScreenStructureNotInferred,
            $"The screen '{name}' is written in TypeScript and JSX, so beyond the file realizing it and the queries it binds - its title, sections, tables, summaries, actions and navigation - nothing about it is inferred",
            @namespace);
}
