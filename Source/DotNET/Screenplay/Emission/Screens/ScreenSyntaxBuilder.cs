// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Screens;

/// <summary>
/// Builds the Screenplay <c>screen</c> declaration for a screen.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The <see cref="TypeReferenceConverter"/> used for the type each binding reads.</param>
/// <remarks>
/// A screen is written with its <c>file</c> reference and its <c>data</c> directives together. The grammar allows a
/// screen to carry both, and both are worth saying - the bindings state what the screen reads, and the file stays
/// the honest pointer to the implementation that no directive replaces. Nothing else is written, because nothing
/// else is known: the widgets of a screen live in JSX, which this generator does not read.
/// </remarks>
public class ScreenSyntaxBuilder(IScreenplayNaming naming, TypeReferenceConverter types)
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

        return new(name, new FileReferenceSyntax(path, SourceLocation.Start), [.. Bindings(screen)], SourceLocation.Start);
    }

    /// <summary>
    /// Gets the path the vertical slice convention would put the file realizing a screen at.
    /// </summary>
    /// <param name="namespace">The namespace of the slice.</param>
    /// <param name="name">The name of the screen.</param>
    /// <returns>The relative path.</returns>
    static string Conventional(string @namespace, string name) =>
        string.Join('/', @namespace.Split('.', StringSplitOptions.RemoveEmptyEntries).Skip(1).Append($"{name}{Extension}"));

    /// <summary>
    /// Builds the <c>data</c> directive of every query the screen binds.
    /// </summary>
    /// <param name="screen">The screen to build for.</param>
    /// <returns>The directives, ordered by the query they read through.</returns>
    IEnumerable<ScreenDirectiveSyntax> Bindings(ScreenModel screen) =>
        screen.Data
            .Select(Binding)
            .OrderBy(_ => _.Query, StringComparer.Ordinal)
            .ThenBy(_ => _.By ?? string.Empty, StringComparer.Ordinal);

    /// <summary>
    /// Builds one <c>data</c> directive.
    /// </summary>
    /// <param name="data">The binding to build for.</param>
    /// <returns>The <see cref="ScreenDataSyntax"/>.</returns>
    /// <remarks>
    /// A <c>data</c> directive has no room for an optional marker - the parser rejects one, so writing it would
    /// produce a document that does not compile - and it needs none, since the query declaration in the same slice
    /// already states what it returns and whether it may return nothing.
    /// </remarks>
    ScreenDataSyntax Binding(ScreenDataModel data) =>
        new(
            types.Convert(data.Type) with { IsOptional = false },
            naming.ToDeclarationName(data.Query),
            data.By is null ? null : naming.ToPropertyName(data.By),
            SourceLocation.Start);
}
