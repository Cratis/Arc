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
/// <param name="data">The <see cref="ScreenDataReader"/> reading which of the slice's queries a screen binds.</param>
/// <param name="elsewhere">The <see cref="CrossSliceQueries"/> told what each slice declares and where it lives.</param>
/// <remarks>
/// Two things about a screen are recovered and no more: the file realizing it, which is what a reader opens, and the
/// queries it binds, which are names the model already holds and can be held against what the slice really declares.
/// The rest of a screen is JSX, and a guessed table or column would be a confident falsehood in a document whose
/// entire value is that it is true.
/// </remarks>
public class ScreenReader(
    IUserInterfaceFiles files,
    SourcePaths paths,
    AmbiguousScreens ambiguity,
    ScreenDataReader data,
    CrossSliceQueries elsewhere)
{
    /// <summary>
    /// Reads the screens of a slice.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="types">The types the slice is declared by.</param>
    /// <param name="queries">The queries the slice declares, under the names it declares them.</param>
    /// <returns>The screens, ordered by name.</returns>
    public IEnumerable<ScreenModel> Read(
        string @namespace,
        IEnumerable<INamedTypeSymbol> types,
        IReadOnlyCollection<QueryModel> queries)
    {
        var directories = SliceDirectories.Of(types);
        elsewhere.Declare(@namespace, directories, queries);

        if (directories.Count == 0)
        {
            return [];
        }

        ambiguity.ReportDirectories(@namespace, directories);

        return Named(@namespace, Found(directories), queries);
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
    /// <returns>The screens, ordered by name.</returns>
    /// <remarks>
    /// A name is what one screen is told apart from another by, so two files claiming the same one leave a document
    /// that says the same word twice and means it differently. The first is kept and the rest are reported.
    /// </remarks>
    IEnumerable<ScreenModel> Named(string @namespace, IEnumerable<string> found, IReadOnlyCollection<QueryModel> queries)
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

            screens.Add(name, new(name, paths.Relative(path) ?? path)
            {
                Data = [.. data.Read(@namespace, name, path, queries)]
            });
        }

        return [.. screens.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }
}
