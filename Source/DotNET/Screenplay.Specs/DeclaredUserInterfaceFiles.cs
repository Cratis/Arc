// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Screens;

namespace Cratis.Arc.Screenplay;

/// <summary>
/// The user interface files a specification says sit alongside the source it compiles, and what they hold.
/// </summary>
/// <remarks>
/// This is what keeps every source analysis specification hermetic once screens are in play - no folder is created,
/// no file is written, and nothing is read from a disk, including the text a component's imports are read out of.
/// Files are returned in the order they were declared rather than sorted, so that a specification declaring them out
/// of order proves the reader orders them itself.
/// </remarks>
public class DeclaredUserInterfaceFiles : IUserInterfaceFiles
{
    /// <summary>
    /// Declares that nothing sits alongside the source.
    /// </summary>
    public static readonly DeclaredUserInterfaceFiles None = new();

    readonly List<(string Path, string? Text)> _files;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredUserInterfaceFiles"/> class holding files whose text no
    /// specification asks for.
    /// </summary>
    /// <param name="paths">The paths of the files, exactly as the specification declares them.</param>
    public DeclaredUserInterfaceFiles(params string[] paths) => _files = [.. paths.Select(_ => (_, (string?)null))];

    DeclaredUserInterfaceFiles(IEnumerable<(string Path, string? Text)> files) => _files = [.. files];

    /// <summary>
    /// Declares files together with the text each one holds.
    /// </summary>
    /// <param name="files">The files, keyed by the path each one sits at.</param>
    /// <returns>The <see cref="DeclaredUserInterfaceFiles"/>.</returns>
    public static DeclaredUserInterfaceFiles Holding(params (string Path, string Text)[] files) =>
        new(files.Select(_ => (_.Path, (string?)_.Text)));

    /// <inheritdoc/>
    public IEnumerable<string> In(string directory) =>
        _files
            .Select(_ => _.Path)
            .Where(_ => ScreenFiles.IsUserInterfaceFile(_) &&
                string.Equals(ScreenFiles.DirectoryOf(_), directory, StringComparison.Ordinal));

    /// <inheritdoc/>
    public string? Contents(string path) =>
        _files.Find(_ => string.Equals(_.Path, path, StringComparison.Ordinal)).Text;
}
