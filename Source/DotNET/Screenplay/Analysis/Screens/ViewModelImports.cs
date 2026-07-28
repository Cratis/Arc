// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Screens;

/// <summary>
/// Reads the imports a view model sitting beside a component writes on its behalf.
/// </summary>
/// <remarks>
/// Where components are written against a view model, the component imports the view model and the view model imports
/// the query - so reading the component alone finds the name of a file and nothing about what the screen reads. That
/// is not a variation worth guessing at in general, but this one is written down: the component names the module, the
/// module sits in the slice's own folder, and what it imports is read exactly the way the component's own imports are.
/// <para>
/// One hop and no further, and only into the folder the screen itself sits in. A view model belongs to the slice that
/// declares it, so following it says what that slice's own screen reads; following a chain of files across folders
/// would be inferring an application's architecture, which is a guess rather than a reading.
/// </para>
/// </remarks>
public static class ViewModelImports
{
    /// <summary>
    /// The ending that marks a module as the view model of a component.
    /// </summary>
    public const string Suffix = "ViewModel";

    static readonly string[] _extensions = [".ts", ".tsx"];

    /// <summary>
    /// Reads the imports the view models a screen names write.
    /// </summary>
    /// <param name="path">The path of the file realizing the screen.</param>
    /// <param name="imports">What the screen itself imports.</param>
    /// <param name="files">The <see cref="IUserInterfaceFiles"/> the text of a module is asked of.</param>
    /// <returns>The imports, in the order the view models write them.</returns>
    public static IEnumerable<ScreenImport> Of(string path, IEnumerable<ScreenImport> imports, IUserInterfaceFiles files)
    {
        var directory = ScreenFiles.DirectoryOf(path);

        return imports
            .Select(_ => Beside(directory, _.Module))
            .OfType<string>()
            .SelectMany(_ => ScreenImports.Statements(TextOf(_, files)));
    }

    /// <summary>
    /// Gets the path of a view model a module specifier names in a directory.
    /// </summary>
    /// <param name="directory">The directory the screen sits in.</param>
    /// <param name="module">The module specifier.</param>
    /// <returns>The path without an extension, or <see langword="null"/> when the module is not one.</returns>
    static string? Beside(string directory, string module) =>
        ModulePaths.Resolve(directory, module) is { } resolved &&
        string.Equals(ScreenFiles.DirectoryOf(resolved), directory, StringComparison.Ordinal) &&
        ScreenFiles.FileNameOf(resolved).EndsWith(Suffix, StringComparison.Ordinal)
            ? resolved
            : null;

    /// <summary>
    /// Gets the text of a module, trying the endings a module is written with.
    /// </summary>
    /// <param name="path">The path of the module, without an extension.</param>
    /// <param name="files">The <see cref="IUserInterfaceFiles"/> the text is asked of.</param>
    /// <returns>The text, or <see langword="null"/> when nothing was found to read.</returns>
    /// <remarks>
    /// A module specifier carries no extension, and a view model holding no markup is written as one file ending and
    /// one holding some as another, so both are asked for in turn. Nothing found is not an error - it is a module
    /// that is not a view model of this slice, or a file that cannot be read, and either way the screen is recovered
    /// from what it says itself.
    /// </remarks>
    static string? TextOf(string path, IUserInterfaceFiles files) =>
        _extensions.Select(_ => files.Contents(path + _)).FirstOrDefault(_ => _ is not null);
}
