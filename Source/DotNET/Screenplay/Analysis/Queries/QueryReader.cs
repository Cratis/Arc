// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Reads the queries a read model or a controller exposes.
/// </summary>
/// <param name="types">The <see cref="TypeRegistry"/> resolving the type of each parameter and return value.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A query returns one instance or many, however the signature dresses that up - awaited, streamed, observed or
/// queryable. The first parameter that has to be given identifies the instance; everything else narrows the result.
/// </remarks>
public class QueryReader(TypeRegistry types, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Determines whether a type is a model-bound read model.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a read model.</returns>
    public static bool IsReadModel(ITypeSymbol type) => type.HasAttribute(WellKnownTypeNames.ReadModelAttribute);

    /// <summary>
    /// Gets the static methods a read model exposes as queries.
    /// </summary>
    /// <param name="type">The read model to read.</param>
    /// <returns>The methods, ordered so that the same read model always reads the same way.</returns>
    public static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol type) =>
        type.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(_ => _ is { MethodKind: MethodKind.Ordinary, IsStatic: true, DeclaredAccessibility: Accessibility.Public } &&
                !_.ReturnsVoid && _.TypeParameters.Length == 0)
            .OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal);

    /// <summary>
    /// Reads a query.
    /// </summary>
    /// <param name="method">The method exposing the query.</param>
    /// <param name="declaring">The type declaring it.</param>
    /// <param name="location">Where the query lives, for use in diagnostics.</param>
    /// <returns>The <see cref="QueryModel"/>, or <see langword="null"/> when what it reads cannot be named.</returns>
    public QueryModel? Read(IMethodSymbol method, INamedTypeSymbol declaring, string location)
    {
        var returnType = ReturnTypeOf(method, location);
        if (returnType is null)
        {
            return null;
        }

        var parameters = method.Parameters.Where(IsInput).ToList();
        var required = parameters.Find(_ => !_.HasExplicitDefaultValue);

        return new(
            method.Name,
            returnType,
            required is null ? null : ToParameter(required),
            [.. parameters.Where(_ => !SymbolEqualityComparer.Default.Equals(_, required)).Select(ToParameter)],
            AuthorizationReader.Read(method, declaring));
    }

    /// <summary>
    /// Determines whether a parameter is part of what a caller sends.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True when the parameter is input rather than infrastructure.</returns>
    /// <remarks>
    /// An interface parameter is a collaborator the host injects, never something a caller can send, so it is not
    /// part of the query's shape.
    /// </remarks>
    static bool IsInput(IParameterSymbol parameter) => parameter.Type.TypeKind != TypeKind.Interface;

    /// <summary>
    /// Reads the type a query returns, stripping every wrapper that only says how it arrives.
    /// </summary>
    /// <param name="method">The method exposing the query.</param>
    /// <param name="location">Where the query lives, for use in diagnostics.</param>
    /// <returns>The <see cref="TypeReferenceModel"/>, or <see langword="null"/> when it cannot be named.</returns>
    /// <remarks>
    /// A transport level result is not a read model, and emitting one as if it were would put a type from the web
    /// framework into a document describing the domain. There is nothing to recover in that case, so the query is
    /// left out and reported rather than described wrongly.
    /// </remarks>
    TypeReferenceModel? ReturnTypeOf(IMethodSymbol method, string location)
    {
        var collection = false;
        var current = QueryReturnTypes.Unwrap(method.ReturnType, ref collection);

        if (QueryReturnTypes.IsTransport(current))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableQuery,
                $"The query '{method.Name}' returns '{current.Name}', which says how the result is transported rather than what it is, so the query was left out",
                location);

            return null;
        }

        if (current.TypeKind == TypeKind.TypeParameter)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableQuery,
                $"The query '{method.Name}' returns a type parameter, which no declaration can name, so the query was left out",
                location);

            return null;
        }

        var resolved = types.Resolve(current);

        return collection ? resolved with { IsCollection = true } : resolved;
    }

    /// <summary>
    /// Converts a parameter of the query.
    /// </summary>
    /// <param name="parameter">The parameter to convert.</param>
    /// <returns>The <see cref="PropertyModel"/>.</returns>
    PropertyModel ToParameter(IParameterSymbol parameter) => new(parameter.Name, types.Resolve(parameter.Type));
}
