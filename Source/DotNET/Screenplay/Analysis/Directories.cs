// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// The arithmetic of directories a document's paths are worked out with.
/// </summary>
/// <remarks>
/// A path in a document is written relative to a directory, and which directory that is comes from comparing the
/// directories the source of a project - or of several - is written in. This is what the comparing is done with, held
/// apart from the paths themselves because the same three questions are asked of one project's directories and of the
/// roots of all of them.
/// </remarks>
public static class Directories
{
    /// <summary>
    /// Normalizes a path onto forward slashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    public static string Normalize(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Finds the longest directory prefix every one of a set of directories shares.
    /// </summary>
    /// <param name="directories">The directories to compare.</param>
    /// <returns>The shared prefix, ending in a separator, empty when they share none.</returns>
    public static string SharedPrefixOf(IReadOnlyList<string> directories)
    {
        if (directories.Count == 0)
        {
            return string.Empty;
        }

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

    /// <summary>
    /// Determines whether a directory is the root of a file system rather than a directory within one.
    /// </summary>
    /// <param name="directory">The directory to check.</param>
    /// <returns>True when it is a file system root.</returns>
    /// <remarks>
    /// Writing a path relative to <c>/</c> is not writing it relative to anything - it removes one character and
    /// leaves the machine's own layout behind, looking like a relative path while being nothing of the sort.
    /// </remarks>
    public static bool IsFileSystemRoot(string directory) =>
        string.Equals(directory, "/", StringComparison.Ordinal) || (directory.Length == 3 && directory[1] == ':' && directory[2] == '/');
}
