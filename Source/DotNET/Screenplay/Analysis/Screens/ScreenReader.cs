// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads the screens a slice ends in from the user interface files sitting alongside its source.
/// </summary>
/// <param name="files">The <see cref="IUserInterfaceFiles"/> telling what sits alongside the source.</param>
/// <param name="paths">The <see cref="SourcePaths"/> rewriting the path of each file.</param>
/// <param name="ambiguity">The <see cref="AmbiguousScreens"/> anything uncertain is reported to.</param>
/// <param name="data">The <see cref="ScreenDataReader"/> resolving the query imports a screen binds.</param>
/// <param name="structure">The <see cref="ScreenStructureReader"/> reading what a screen states through known Cratis Components.</param>
/// <param name="elsewhere">The <see cref="CrossSliceQueries"/> told what each slice declares and where it lives.</param>
/// <remarks>
/// What is recovered about a screen is what can be read without guessing: the file realizing it, which is what a
/// reader opens, the queries it binds, which are names the model already holds, and the structure it states through
/// known Cratis Components. The rest of a screen is JSX, and a guessed table or column would be a confident falsehood
/// in a document whose entire value is that it is true.
/// </remarks>
public class ScreenReader(
    IUserInterfaceFiles files,
    SourcePaths paths,
    AmbiguousScreens ambiguity,
    ScreenDataReader data,
    ScreenStructureReader structure,
    CrossSliceQueries elsewhere)
{
    /// <summary>
    /// Finds the screens of a slice and defers their directives until every slice has declared its proxies.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="types">The types the slice is declared by.</param>
    /// <param name="queries">The queries the slice declares, under the names it declares them.</param>
    /// <param name="commands">The commands the slice declares.</param>
    /// <returns>The screen identities, ordered by name, whose directives <see cref="CrossSliceQueries.Apply"/> resolves.</returns>
    public IEnumerable<ScreenModel> Read(
        string @namespace,
        IEnumerable<INamedTypeSymbol> types,
        IReadOnlyCollection<QueryModel> queries,
        IReadOnlyCollection<CommandModel> commands)
    {
        var directories = SliceDirectories.Of(types);
        elsewhere.Declare(@namespace, directories, queries, commands);

        if (directories.Count == 0)
        {
            return [];
        }

        ambiguity.ReportDirectories(@namespace, directories);

        return Named(@namespace, Found(directories), queries, commands);
    }

    /// <summary>
    /// Finds every user interface file sitting in a set of directories.
    /// </summary>
    /// <param name="directories">The directories to look in.</param>
    /// <returns>The paths, ordered so that the same source always reads the same way.</returns>
    IEnumerable<string> Found(IReadOnlyList<string> directories) =>
        directories
            .SelectMany(files.In)
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);

    /// <summary>
    /// Names the screen every file realizes, leaving out the files that realize none.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="found">The paths found alongside the source.</param>
    /// <param name="queries">The queries the slice declares.</param>
    /// <param name="commands">The commands the slice declares.</param>
    /// <returns>The screens, ordered by name.</returns>
    /// <remarks>
    /// A name is what one screen is told apart from another by, so two files claiming the same one leave a document
    /// that says the same word twice and means it differently. The first is kept and the rest are reported.
    /// </remarks>
    IEnumerable<ScreenModel> Named(
        string @namespace,
        IEnumerable<string> found,
        IReadOnlyCollection<QueryModel> queries,
        IReadOnlyCollection<CommandModel> commands)
    {
        var screens = new Dictionary<string, ScreenModel>(StringComparer.Ordinal);

        foreach (var path in found)
        {
            if (ScreenFiles.NameOf(path) is not { } name)
            {
                continue;
            }

            if (screens.TryGetValue(name, out var kept))
            {
                ambiguity.ReportRepeatedName(@namespace, paths.Relative(path) ?? path, kept.FilePath, name);
                continue;
            }

            var screen = new ScreenModel(name, paths.Relative(path) ?? path);
            screens.Add(name, screen);
            elsewhere.DeclareScreen(screen, @namespace, path);
            elsewhere.Defer(screen, () =>
            {
                var components = KnownComponents.In(files.Contents(path) ?? string.Empty);
                var resolved = new ScreenProxyReferences(@namespace, path, components, queries, commands, elsewhere);
                var stated = structure.Read(@namespace, name, path, queries, commands, resolved, route => elsewhere.Navigation(@namespace, route));
                return screen with
                {
                    Data = [.. data.Read(@namespace, path)],
                    Titles = stated.Titles,
                    Toolbars = stated.Toolbars,
                    Tables = stated.Tables,
                    Actions = stated.Actions,
                    AfterSuccess = stated.AfterSuccess
                };
            });
        }

        return [.. screens.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }
}
