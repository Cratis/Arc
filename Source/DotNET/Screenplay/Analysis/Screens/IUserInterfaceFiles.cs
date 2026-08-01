// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Defines a system that tells which user interface files a directory holds.
/// </summary>
/// <remarks>
/// Analysis works from a compilation and nothing else, which is what makes it hermetic and what lets every
/// specification be source strings compiled in memory. A screen is the one thing a compilation cannot answer for -
/// the file realizing it is TypeScript, so no syntax tree carries it - and this is the single seam where that
/// question is asked. Substituting it keeps analysis as testable as the rest.
/// </remarks>
public interface IUserInterfaceFiles
{
    /// <summary>
    /// Gets the user interface files a directory holds, without descending into it.
    /// </summary>
    /// <param name="directory">The directory to read, spelled the way the compilation spells its source paths.</param>
    /// <returns>The paths of the files, in no particular order.</returns>
    /// <remarks>
    /// Descending is deliberately not done. A folder within a slice folder is a slice of its own under the vertical
    /// slice convention, and its files belong to it rather than to the slice above.
    /// </remarks>
    IEnumerable<string> In(string directory);

    /// <summary>
    /// Gets the text of a user interface file.
    /// </summary>
    /// <param name="path">The path of the file, spelled exactly as <see cref="In"/> gave it.</param>
    /// <returns>The text, or <see langword="null"/> when the file cannot be read.</returns>
    /// <remarks>
    /// A component's import statements name the queries it binds, which is the one thing about a screen the model
    /// already knows enough to check. Asking for the text through the same seam that answered which files exist is
    /// what keeps every specification hermetic - a specification answers both questions itself, and nothing in
    /// analysis ever reaches around to a disk.
    /// <para>
    /// A file listed a moment ago may be unreadable now, and a host may know the paths without holding the text at
    /// all. Neither is an error - the screen is then recovered from its file reference alone.
    /// </para>
    /// </remarks>
    string? Contents(string path);
}
