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
            IsObservable: query.IsObservable);

    /// <summary>
    /// Converts a parameter of the query.
    /// </summary>
    /// <param name="parameter">The parameter to convert.</param>
    /// <returns>The <see cref="QueryParameterSyntax"/>.</returns>
    QueryParameterSyntax ToParameter(PropertyModel parameter) =>
        new(naming.ToPropertyName(parameter.Name), types.Convert(parameter.Type), SourceLocation.Start);
}
