// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Screens;

/// <summary>
/// Builds the Screenplay <c>screen</c> declaration for a screen.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// A screen is written in its <c>file</c> form and never in its declarative one. The declarative form describes what
/// a screen shows and does, and nothing in a compilation says either - so the file reference is the whole of what is
/// honestly known, and it is the part a reader most wants anyway.
/// </remarks>
public class ScreenSyntaxBuilder(IScreenplayNaming naming)
{
    /// <summary>
    /// The extension of the file realizing a screen.
    /// </summary>
    public const string Extension = ".tsx";

    /// <summary>
    /// Builds the screen declaration.
    /// </summary>
    /// <param name="screen">The screen to build for.</param>
    /// <param name="namespace">The namespace of the slice the screen belongs to.</param>
    /// <returns>The <see cref="ScreenSyntax"/>.</returns>
    /// <remarks>
    /// A screen with neither a file nor a directive has an empty body, so a path is always resolved - falling back
    /// to where the vertical slice convention would put the file when the model carries none.
    /// </remarks>
    public ScreenSyntax Build(ScreenModel screen, string @namespace)
    {
        var name = naming.ToDeclarationName(screen.Name);
        var path = naming.ToFilePath(screen.FilePath) ?? Conventional(@namespace, name);

        return new(name, new FileReferenceSyntax(path, SourceLocation.Start), [], SourceLocation.Start);
    }

    /// <summary>
    /// Gets the path the vertical slice convention would put the file realizing a screen at.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="name">The name of the screen.</param>
    /// <returns>The relative path.</returns>
    static string Conventional(string @namespace, string name) =>
        string.Join('/', @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Skip(1).Append($"{name}{Extension}"));
}
