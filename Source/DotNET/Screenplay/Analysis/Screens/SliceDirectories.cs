// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Resolves the directories the source of a slice lives in.
/// </summary>
/// <remarks>
/// This is what a syntax tree buys that metadata never could - a real path - and it is the whole reason a screen can
/// be recovered at all. The vertical slice convention puts the file realizing a screen next to the source of the
/// slice it belongs to, so where the source lives is where to look.
/// <para>
/// What a source generator emitted is not where the source lives. A logging generator writing partial members into
/// the intermediate folder of the build contributes symbols to the slice and nothing at all to the question of where
/// its screens are, so a slice sitting in one folder would otherwise be reported as spread over two and its screens
/// looked for in a build folder.
/// </para>
/// </remarks>
public static class SliceDirectories
{
    /// <summary>
    /// Gets the directories a set of types is written across.
    /// </summary>
    /// <param name="types">The types to locate.</param>
    /// <returns>The directories, distinct and ordered.</returns>
    public static IReadOnlyList<string> Of(IEnumerable<INamedTypeSymbol> types) =>
    [
        .. types
            .SelectMany(_ => _.DeclaringSyntaxReferences)
            .Select(_ => _.SyntaxTree.FilePath)
            .Where(_ => !string.IsNullOrWhiteSpace(_) && !GeneratedSource.Is(_))
            .Select(ScreenFiles.DirectoryOf)
            .Where(_ => _.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];
}
