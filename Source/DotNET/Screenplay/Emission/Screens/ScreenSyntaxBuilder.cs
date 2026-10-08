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
/// A screen is written with its <c>file</c> reference and its directives together. The grammar allows a screen to
/// carry both, and both are worth saying - the directives state what the screen reads and shows, and the file stays
/// the honest pointer to the implementation that no directive replaces. The <c>data</c> bindings come first, then the
/// titles, tables and actions read from the Cratis Components the screen uses. Nothing else is written, because
/// nothing else is known.
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

        ScreenDirectiveSyntax[] directives =
        [
            .. Bindings(screen),
            .. screen.Titles.Select(_ => new ScreenTitleSyntax(_, SourceLocation.Start)),
            .. screen.Tables.Select(Table),
            .. screen.Actions.Select(_ => new ScreenActionSyntax(naming.ToDeclarationName(_), null, null, SourceLocation.Start))
        ];

        return new(name, new FileReferenceSyntax(path, SourceLocation.Start), directives, SourceLocation.Start);
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

    /// <summary>
    /// Builds one <c>table</c> directive.
    /// </summary>
    /// <param name="table">The table to build for.</param>
    /// <returns>The <see cref="ScreenTableSyntax"/>.</returns>
    /// <remarks>
    /// The table is named after the read model its rows are, under the name the document declares it by. A column is
    /// written with the property exactly as the component names it, because that is the property the component shows.
    /// </remarks>
    ScreenTableSyntax Table(ScreenTableModel table) =>
        new(
            types.Convert(new TypeReferenceModel(table.ReadModel, false, false)).Name,
            [.. table.Columns.Select(_ => new ScreenColumnSyntax(_.Property, _.Label, SourceLocation.Start))],
            null,
            SourceLocation.Start);
}
