// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Resolves the directory every path in a document is written relative to, across the projects of an application.
/// </summary>
/// <remarks>
/// One project answers this on its own - the directory its own source sits under. Several projects each answer it for
/// themselves, and writing each project's paths relative to its own root would leave a document where <c>Ordering.cs</c>
/// is the path of two files in two projects and nothing says which. The directory the projects are all written under
/// is the one that keeps a path both relative and unambiguous, so that is what is used and every path in the document
/// then opens with the project it belongs to.
/// <para>
/// It is not always there. Projects checked out beside each other in unrelated places share nothing but the root of
/// the file system, and writing a path relative to that leaves the machine's own layout behind while looking like a
/// relative path. Each project's own root is used instead, which is the ambiguity above rather than a document nobody
/// can commit, and it is reported so that the ambiguity is one the reader knows about.
/// </para>
/// </remarks>
public static class SourceRoots
{
    /// <summary>
    /// Resolves what the paths of each compilation are written relative to.
    /// </summary>
    /// <param name="compilations">The compilations being analyzed, ordered.</param>
    /// <param name="catalogs">What each of them declares, in the same order.</param>
    /// <param name="diagnostics">The diagnostics to report to.</param>
    /// <param name="location">Where to report against.</param>
    /// <returns>The <see cref="SourcePaths"/> of each compilation, in the same order.</returns>
    public static IReadOnlyList<SourcePaths> Across(
        IReadOnlyList<Compilation> compilations,
        IReadOnlyList<ArtifactCatalog> catalogs,
        ScreenplayDiagnostics diagnostics,
        string? location)
    {
        var roots = compilations.Select((compilation, index) => SourcePaths.RootOf(compilation, catalogs[index])).ToList();
        if (SharedBy(roots) is { } shared)
        {
            return [.. roots.Select(_ => new SourcePaths(shared))];
        }

        diagnostics.Warning(
            ScreenplayDiagnosticCodes.ProjectsWithoutASharedRoot,
            $"The {roots.Count} projects of the application are written in directories that share none, so every path is written relative to the root of the project it belongs to and says nothing about which project that is",
            location);

        return [.. roots.Select(_ => new SourcePaths(_))];
    }

    /// <summary>
    /// Gets the directory the source of every project sits under.
    /// </summary>
    /// <param name="roots">The root of each project.</param>
    /// <returns>The directory, or <see langword="null"/> when the projects share none.</returns>
    /// <remarks>
    /// A single project is its own answer whatever that answer is, including the empty one it gives when there is
    /// nothing to write its paths relative to - that is one project's situation rather than several projects failing
    /// to agree, and it is what a document of one project has always said.
    /// </remarks>
    static string? SharedBy(List<string> roots)
    {
        if (roots.Count <= 1)
        {
            return roots.Count == 0 ? string.Empty : roots[0];
        }

        var shared = Directories.SharedPrefixOf(roots);

        return shared.Length == 0 || Directories.IsFileSystemRoot(shared) ? null : shared;
    }
}
