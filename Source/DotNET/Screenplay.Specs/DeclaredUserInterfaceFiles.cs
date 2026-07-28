// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// The user interface files a specification says sit alongside the source it compiles.
/// </summary>
/// <param name="paths">The paths of the files, exactly as the specification declares them.</param>
/// <remarks>
/// This is what keeps every source analysis specification hermetic once screens are in play - no folder is created,
/// no file is written, and nothing is read from a disk. Files are returned in the order they were declared rather
/// than sorted, so that a specification declaring them out of order proves the reader orders them itself.
/// </remarks>
public class DeclaredUserInterfaceFiles(params string[] paths) : IUserInterfaceFiles
{
    /// <summary>
    /// Declares that nothing sits alongside the source.
    /// </summary>
    public static readonly DeclaredUserInterfaceFiles None = new();

    /// <inheritdoc/>
    public IEnumerable<string> In(string directory) =>
        paths.Where(_ => ScreenFiles.IsUserInterfaceFile(_) &&
            string.Equals(ScreenFiles.DirectoryOf(_), directory, StringComparison.Ordinal));
}
