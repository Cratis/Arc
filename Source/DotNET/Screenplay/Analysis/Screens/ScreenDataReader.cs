// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads the declared queries a screen imports from its own or another slice.
/// </summary>
/// <param name="files">The <see cref="IUserInterfaceFiles"/> the text of a component is asked of.</param>
/// <param name="elsewhere">The import-site declaration catalog shared across slices.</param>
/// <remarks>
/// Arc generates a proxy per query and a component imports it by name, so an import is a name the model can be held
/// against rather than a reading of a user interface. An import whose owner declares that query is a binding.
/// Nothing about the binding is taken from the component beyond the name - the type and the key come from the query,
/// which is C#.
/// <para>
/// Imports are resolved through their module paths against every slice's declarations. Cross-slice bindings retain
/// the owner's namespace until emission replaces it with the shortest unambiguous emitted scope.
/// </para>
/// <para>
/// What a view model beside the component imports counts as what the component imports, because where components are
/// written that way the query is named there and the component names only the view model - see
/// <see cref="ViewModelImports"/> for how far that is followed.
/// </para>
/// <para>
/// The rest of the declarative form - titles, tables, columns and actions - is read by
/// <see cref="ScreenStructureReader"/>, which also reports what of it stays unread.
/// </para>
/// </remarks>
public class ScreenDataReader(IUserInterfaceFiles files, CrossSliceQueries elsewhere)
{
    /// <summary>
    /// Reads the bindings of one screen.
    /// </summary>
    /// <param name="namespace">The namespace of the slice the screen belongs to.</param>
    /// <param name="path">The path of the file realizing it, as the compilation spells it.</param>
    /// <returns>The bindings, ordered by the query they read through.</returns>
    public IEnumerable<ScreenDataModel> Read(
        string @namespace,
        string path)
    {
        var written = ScreenImports.Statements(files.Contents(path));
        var imports = written.Concat(ViewModelImports.Of(path, written, files)).Distinct().ToList();
        return
        [
            .. imports.Select(import => elsewhere.Query(@namespace, path, import))
                .OfType<ScreenDataModel>()
                .Distinct()
                .OrderBy(_ => _.Query, StringComparer.Ordinal)
        ];
    }
}
