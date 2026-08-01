// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Emission.Files;

/// <summary>
/// Resolves the path a <c>file</c> reference points at.
/// </summary>
/// <remarks>
/// Analysis carries the real path from the syntax tree whenever it has one. When it does not - an artifact living
/// in a referenced package has metadata but no source - the vertical slice convention, where the namespace mirrors
/// the folder structure, is the closest thing to an answer.
/// </remarks>
public static class SourceFilePaths
{
    /// <summary>
    /// The extension of a source file.
    /// </summary>
    public const string Extension = ".cs";

    /// <summary>
    /// Gets the conventional path of an artifact from the namespace it lives in.
    /// </summary>
    /// <param name="namespace">The namespace of the artifact.</param>
    /// <param name="name">The name of the artifact.</param>
    /// <returns>The relative path.</returns>
    public static string Conventional(string @namespace, string name)
    {
        var folders = @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Skip(1);

        return string.Join('/', folders.Append($"{name}{Extension}"));
    }
}
