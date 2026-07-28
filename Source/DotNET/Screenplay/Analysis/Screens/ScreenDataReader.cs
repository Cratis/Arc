// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads which of a slice's queries a screen binds, and says plainly what it does not read.
/// </summary>
/// <param name="files">The <see cref="IUserInterfaceFiles"/> the text of a component is asked of.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything not inferred is reported to.</param>
/// <remarks>
/// Arc generates a proxy per query and a component imports it by name, so an import is a name the model can be held
/// against rather than a reading of a user interface. A name matching a query the slice declares is a binding; a
/// name matching nothing is dropped, whatever the component does with it. Nothing about the binding is taken from
/// the component beyond the name - the type and the key come from the query, which is C#.
/// <para>
/// Every screen also reports what stays out. The rest of the declarative form is JSX structure, and the cost of
/// guessing it wrong is a document that states something about the application that is not so - which is worse than
/// a document that says less. Saying so per screen is what turns that limit from a silence into an answer.
/// </para>
/// </remarks>
public class ScreenDataReader(IUserInterfaceFiles files, ScreenplayDiagnostics diagnostics)
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
        var imported = ScreenImports.In(files.Contents(path));

        ReportUninferredStructure(@namespace, name);

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
