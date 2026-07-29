// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Turns the absolute paths a compilation carries into paths a generated document can reference.
/// </summary>
/// <param name="root">The directory every path is written relative to.</param>
/// <remarks>
/// Syntax trees carry whatever path the compiler was handed, which for a real build is absolute and specific to
/// the machine it ran on. A document is meant to be committed and diffed, so paths are written relative to the
/// deepest directory every source file shares.
/// </remarks>
public class SourcePaths(string root)
{
    /// <summary>
    /// Resolves the shared root of the source an application is really written in.
    /// </summary>
    /// <param name="compilation">The compilation to read.</param>
    /// <param name="catalog">The catalogue of everything the compilation declares.</param>
    /// <returns>The <see cref="SourcePaths"/>.</returns>
    /// <remarks>
    /// A compilation carries more than the project's own files. A referenced package can ship source of its own -
    /// a file of global using directives is the common case - and it is compiled from wherever the package cache
    /// happens to live. Taking the shared prefix of every syntax tree then yields the file system root, and every
    /// path in the document comes out as the absolute layout of the machine that generated it, which is exactly what
    /// stops a document from being committed and diffed.
    /// <para>
    /// The files declaring the artifacts say where the project is: the deepest directory most of them sit under is
    /// certainly inside it. Every file on that same path - a folder beside them, a program entry point above them -
    /// belongs to the project too and widens the root; a file sharing nothing with them but the file system root
    /// belongs to somebody else and is left out of the question entirely.
    /// </para>
    /// </remarks>
    public static SourcePaths For(Compilation compilation, ArtifactCatalog catalog)
    {
        var declared = DirectoriesOf(catalog.Types.Select(_ => _.SourceFilePath()).Where(_ => !GeneratedSource.Is(_)));
        var anchor = DeepestSharedBy(declared);
        var project = Rooted(declared, anchor);
        var root = Rooted(DirectoriesOf(compilation.SyntaxTrees.Select(_ => _.FilePath).Where(_ => !GeneratedSource.Is(_))), project);

        return new(IsFileSystemRoot(root) ? string.Empty : root);
    }

    /// <summary>
    /// Rewrites a path so that it is relative to the shared root.
    /// </summary>
    /// <param name="path">The path to rewrite.</param>
    /// <returns>The relative path, or <see langword="null"/> when there is none.</returns>
    public string? Relative(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalized = Normalize(path);

        return root.Length > 0 && normalized.StartsWith(root, StringComparison.Ordinal)
            ? normalized[root.Length..]
            : normalized;
    }

    /// <summary>
    /// Gets the deepest directory that most of a set of directories sit under.
    /// </summary>
    /// <param name="directories">The directories to weigh.</param>
    /// <returns>The directory, empty when there is none.</returns>
    /// <remarks>
    /// This exists to be unmoved by a stray file. A directory holding most of what the application declares is
    /// somewhere inside the project whatever else the compilation was handed, which is enough to tell the project
    /// from everything sitting outside it.
    /// </remarks>
    static string DeepestSharedBy(List<string> directories) =>
        directories
            .SelectMany(Ancestors)
            .Distinct(StringComparer.Ordinal)
            .Where(candidate => directories.Count(_ => _.StartsWith(candidate, StringComparison.Ordinal)) * 2 > directories.Count)
            .OrderByDescending(_ => _.Length)
            .ThenBy(_ => _, StringComparer.Ordinal)
            .FirstOrDefault() ?? string.Empty;

    /// <summary>
    /// Gets every directory a directory sits under, itself included.
    /// </summary>
    /// <param name="directory">The directory to walk up from.</param>
    /// <returns>The directories, each ending in a separator.</returns>
    static IEnumerable<string> Ancestors(string directory)
    {
        for (var separator = directory.IndexOf('/', StringComparison.Ordinal); separator >= 0; separator = directory.IndexOf('/', separator + 1))
        {
            yield return directory[..(separator + 1)];
        }
    }

    /// <summary>
    /// Gets the directory shared by everything on the same path as an anchor.
    /// </summary>
    /// <param name="directories">The directories to reduce.</param>
    /// <param name="anchor">The directory known to be within the project.</param>
    /// <returns>The shared directory, empty when there is none.</returns>
    static string Rooted(List<string> directories, string anchor)
    {
        var onThePath = directories.Where(_ => OnTheSamePath(_, anchor)).ToList();

        return onThePath.Count == 0 ? string.Empty : CommonPrefix(onThePath);
    }

    /// <summary>
    /// Gets the distinct directories a set of paths live in, in a deterministic order.
    /// </summary>
    /// <param name="paths">The paths to read.</param>
    /// <returns>The directories, each ending in a separator.</returns>
    static List<string> DirectoriesOf(IEnumerable<string?> paths) =>
    [
        .. paths
            .Where(_ => !string.IsNullOrWhiteSpace(_))
            .Select(_ => Normalize(_!))
            .Select(_ => _[..(_.LastIndexOf('/') + 1)])
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
    ];

    /// <summary>
    /// Determines whether a directory and the project's own are on the same path.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <param name="project">The directory the artifacts are declared under.</param>
    /// <returns>True when one contains the other.</returns>
    static bool OnTheSamePath(string directory, string project) =>
        project.Length == 0 ||
        directory.StartsWith(project, StringComparison.Ordinal) ||
        project.StartsWith(directory, StringComparison.Ordinal);

    /// <summary>
    /// Determines whether a directory is the root of a file system rather than a directory within one.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <returns>True when it is a file system root.</returns>
    /// <remarks>
    /// Writing a path relative to <c>/</c> is not writing it relative to anything - it removes one character and
    /// leaves the machine's own layout behind, looking like a relative path while being nothing of the sort.
    /// </remarks>
    static bool IsFileSystemRoot(string directory) =>
        string.Equals(directory, "/", StringComparison.Ordinal) || (directory.Length == 3 && directory[1] == ':' && directory[2] == '/');

    /// <summary>
    /// Normalizes a path onto forward slashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Finds the longest directory prefix every path shares.
    /// </summary>
    /// <param name="directories">The directories to compare.</param>
    /// <returns>The shared prefix, ending in a separator.</returns>
    static string CommonPrefix(List<string> directories)
    {
        var prefix = directories[0];

        foreach (var directory in directories.Skip(1))
        {
            var length = 0;
            while (length < prefix.Length && length < directory.Length && prefix[length] == directory[length])
            {
                length++;
            }

            prefix = prefix[..length];
        }

        var separator = prefix.LastIndexOf('/');

        return separator < 0 ? string.Empty : prefix[..(separator + 1)];
    }
}
