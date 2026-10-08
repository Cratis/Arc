// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Policies;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Queries;

/// <summary>
/// Builds the Screenplay <c>query</c> declaration for a query.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="types">The <see cref="TypeReferenceConverter"/> used for the return and parameter types.</param>
/// <param name="authorize">The <see cref="AuthorizeSyntaxBuilder"/> used for the authorize block.</param>
public class QuerySyntaxBuilder(
    IScreenplayNaming naming,
    TypeReferenceConverter types,
    AuthorizeSyntaxBuilder authorize)
{
    /// <summary>
    /// Gets whether unadmitted performer metadata should be emitted.
    /// </summary>
    public bool AuthoringOnlyConstructs { get; init; }

    /// <summary>
    /// Gets where withheld metadata is reported.
    /// </summary>
    public ScreenplayDiagnostics? Diagnostics { get; init; }

    /// <summary>
    /// Builds the query declaration.
    /// </summary>
    /// <param name="query">The query to build for.</param>
    /// <returns>The <see cref="QuerySyntax"/>.</returns>
    public QuerySyntax Build(QueryModel query) =>
        new(
            naming.ToDeclarationName(query.Name),
            types.Convert(query.ReturnType),
            query.By is null ? null : ToParameter(query.By),
            [.. query.Filters.Select(ToParameter)],
            authorize.Build(query.Authorization),
            SourceLocation.Start,
            Description: naming.ToStringLiteral(query.Description),
            Performer: PerformerOf(query),
            IsObservable: query.IsObservable);

    /// <summary>
    /// Builds the authoring-only implementation reference.
    /// </summary>
    /// <param name="query">The query to convert.</param>
    /// <returns>The performer, or null when no file was recovered.</returns>
    PerformerSyntax? PerformerOf(QueryModel query)
    {
        if (naming.ToFilePath(query.PerformerFile) is not { } path)
        {
            return null;
        }

        if (AuthoringOnlyConstructs)
        {
            return new(new FileReferenceSyntax(path, SourceLocation.Start), null, SourceLocation.Start);
        }

        Diagnostics?.Information(
            ScreenplayDiagnosticCodes.UnmappableQuery,
            $"The query '{query.Name}' has a body; performer references are authoring-only, so enable ScreenplayOptions.AuthoringOnlyConstructs to include its implementation file",
            query.PerformerFile);

        return null;
    }

    /// <summary>
    /// Converts a query parameter.
    /// </summary>
    /// <param name="parameter">The parameter to convert.</param>
    /// <returns>The parameter syntax.</returns>
    QueryParameterSyntax ToParameter(PropertyModel parameter) =>
        new(naming.ToPropertyName(parameter.Name), types.Convert(parameter.Type), SourceLocation.Start);
}
