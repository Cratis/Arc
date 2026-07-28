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
/// <remarks>
/// Only the file realizing a screen is recovered, never its structure. A screen's structure is TypeScript and JSX,
/// and inferring <c>data</c>, <c>table</c> or <c>action</c> directives would mean reading a language this generator
/// does not read - so it says the one thing it actually knows, which is which file a reader should open.
/// </remarks>
public class ScreenReader(IUserInterfaceFiles files, SourcePaths paths, AmbiguousScreens ambiguity)
{
    /// <summary>
    /// Reads the screens of a slice.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="types">The types the slice is declared by.</param>
    /// <returns>The screens, ordered by name.</returns>
    public IEnumerable<ScreenModel> Read(string @namespace, IEnumerable<INamedTypeSymbol> types)
    {
        var directories = SliceDirectories.Of(types);
        if (directories.Count == 0)
        {
            return [];
        }

        ambiguity.ReportDirectories(@namespace, directories);

        return Named(@namespace, Found(directories));
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
    /// <returns>The screens, ordered by name.</returns>
    /// <remarks>
    /// A name is what one screen is told apart from another by, so two files claiming the same one leave a document
    /// that says the same word twice and means it differently. The first is kept and the rest are reported.
    /// </remarks>
    IEnumerable<ScreenModel> Named(string @namespace, IEnumerable<string> found)
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

            screens.Add(name, new(name, paths.Relative(path) ?? path));
        }

        return [.. screens.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }
}
