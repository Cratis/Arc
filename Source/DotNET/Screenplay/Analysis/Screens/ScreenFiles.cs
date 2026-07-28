// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Recognizes the files that realize a screen and reads the directory and the name of one.
/// </summary>
/// <remarks>
/// Paths are compared and split on forward slashes only. A compilation carries whatever separator the machine it
/// was built on uses, and a document is written once and read everywhere, so both are normalized before anything
/// looks at them.
/// </remarks>
public static class ScreenFiles
{
    /// <summary>
    /// The extension of a file realizing a screen.
    /// </summary>
    public const string Extension = ".tsx";

    /// <summary>
    /// Determines whether a path names a user interface file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True when the path names one.</returns>
    public static bool IsUserInterfaceFile(string path) =>
        path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the name of the screen a file realizes.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The name, or <see langword="null"/> when the file realizes no screen.</returns>
    /// <remarks>
    /// A component is named after what it is, so its file name is the name of the screen. A name carrying a second
    /// extension - <c>.stories.tsx</c>, <c>.spec.tsx</c>, <c>.test.tsx</c> - names a companion of a component
    /// rather than a component, and a companion is not something a slice ends in.
    /// </remarks>
    public static string? NameOf(string path)
    {
        if (!IsUserInterfaceFile(path))
        {
            return null;
        }

        var name = FileNameOf(path);
        name = name[..^Extension.Length];

        return name.Length == 0 || name.Contains('.', StringComparison.Ordinal) ? null : name;
    }

    /// <summary>
    /// Gets the directory a file sits in.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <returns>The directory, without a trailing separator, empty when the path names no directory.</returns>
    public static string DirectoryOf(string path)
    {
        var normalized = Normalize(path);
        var separator = normalized.LastIndexOf('/');

        return separator < 0 ? string.Empty : normalized[..separator];
    }

    /// <summary>
    /// Gets the name of the file a path ends in.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <returns>The file name.</returns>
    static string FileNameOf(string path)
    {
        var normalized = Normalize(path);

        return normalized[(normalized.LastIndexOf('/') + 1)..];
    }

    /// <summary>
    /// Normalizes a path onto forward slashes.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>The normalized path.</returns>
    static string Normalize(string path) => path.Replace('\\', '/');
}
