// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Emission.Naming;
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
/// queryable. The first required parameter identifies an instance only when its emitted name matches a property
/// of what the query returns; otherwise it narrows the result without claiming a key the read model does not hold.
/// </remarks>
public class QueryReader(TypeRegistry types, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The types a query is handed by the host it runs in rather than by the caller it answers.
    /// </summary>
    public static readonly string[] InfrastructureTypes =
    [
        WellKnownTypeNames.CancellationToken,
        WellKnownTypeNames.QueryContext,
        WellKnownTypeNames.Paging,
        WellKnownTypeNames.Sorting
    ];

    readonly ScreenplayNaming _naming = new();

    /// <summary>
    /// Gets the source paths used for performer references.
    /// </summary>
    public SourcePaths? Paths { get; init; }

    /// <summary>
    /// Gets the semantic models used to prove equality filters in query bodies.
    /// </summary>
    public SemanticModels? Models { get; init; }

    /// <summary>
    /// Determines whether a type is a model-bound read model.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a read model.</returns>
    public static bool IsReadModel(ITypeSymbol type) => ModelBoundQuerySelection.IsReadModel(type);

    /// <summary>
    /// Gets the static methods a read model exposes as queries.
    /// </summary>
    /// <param name="type">The read model to read.</param>
    /// <returns>The methods, ordered so that the same read model always reads the same way.</returns>
    /// <remarks>
    /// Arc exposes only public static methods returning the read model or a supported wrapper around it.
    /// Non-public helpers are never queries, even when marked, while the declaring read-model type may be internal.
    /// Explicit selection additionally requires the query marker. Helpers must not appear as routes in the document.
    /// </remarks>
    public static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol type) => ModelBoundQuerySelection.MethodsOf(type);

    /// <summary>
    /// Determines whether a parameter is part of what a caller sends.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True when the parameter is input rather than infrastructure.</returns>
    /// <remarks>
    /// Arc decides this at run time by asking the container whether the parameter's type is a service, which is not a
    /// question source can answer - so what is asked instead is whether a caller could possibly send one. An interface
    /// and an abstract type both fail that outright: neither has a value to send, and both are how a collaborator the
    /// host injects is written - <c>TimeProvider</c>, the clock a query measures a threshold against, is abstract
    /// rather than an interface and reached the document as a parameter no caller has ever sent. Being uninstantiable
    /// is not the whole of it though: a cancellation token, the page asked for and the order asked for are all filled
    /// in by the host from the request, and all three are concrete values. Stating any of them as caller input puts a
    /// parameter in the document that no caller sends, typed by a name the document never declares.
    /// <para>
    /// A concrete class the container happens to resolve still comes through as input, because nothing in the source
    /// tells it apart from a value a caller sends. That is the residue of approximating a container lookup statically,
    /// and it is the narrow side of the trade: a parameter wrongly stated is visible in the document, while one
    /// wrongly dropped is not.
    /// </para>
    /// <para>
    /// An interface parameter is never input by the same reasoning - there is no value a caller could have sent for
    /// it, only a collaborator the host resolves - which is what excludes <c>IReadModels</c> from a recovered
    /// specification's own arguments without naming it specially.
    /// </para>
    /// </remarks>
    public static bool IsInput(IParameterSymbol parameter) =>
        parameter.Type is { TypeKind: not TypeKind.Interface, IsAbstract: false } &&
        !Array.Exists(InfrastructureTypes, parameter.Type.Is);

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

        ReportHowItIsServed(method, location);

        var parameters = method.Parameters.Where(IsInput).ToList();
        var required = parameters.Find(_ => !_.HasExplicitDefaultValue);
        var collection = false;
        var returned = UnderlyingTypes.Of(QueryReturnTypes.Unwrap(method.ReturnType, ref collection));
        var keyProperty = required is null ? null : returned.DeclaredProperties().FirstOrDefault(property =>
            string.Equals(_naming.ToPropertyName(property.Name), _naming.ToPropertyName(required.Name), StringComparison.Ordinal));
        var key = keyProperty is null ? null : required;
        var keyType = key?.Type;
        if (keyType.Is(WellKnownTypeNames.EventSourceId) && IsPrimitiveKeyType(keyProperty?.Type))
        {
            keyType = keyProperty?.Type;
        }

        return new(
            method.Name,
            returnType,
            key is null ? null : ToParameter(key, keyType),
            new QueryFilterReader(types, Models, diagnostics).Read(method, key, location).ToList(),
            AuthorizationReader.Read(method, declaring),
            QueryReturnTypes.IsObservable(method.ReturnType))
        {
            ReturnTypeFullName = FullNameOfWhatItReturns(method),
            Description = Documentation.SummaryOf(method),
            PerformerFile = new QueryImplementationReader(Paths, diagnostics).Read(method, location)
        };
    }

    /// <summary>
    /// Gets the full name of the type a query returns, every wrapper saying how it arrives stripped.
    /// </summary>
    /// <param name="method">The method exposing the query.</param>
    /// <returns>The full name.</returns>
    static string FullNameOfWhatItReturns(IMethodSymbol method)
    {
        var collection = false;

        return UnderlyingTypes.Of(QueryReturnTypes.Unwrap(method.ReturnType, ref collection)).ToDisplayString();
    }

    /// <summary>
    /// Determines whether a property's type is a primitive key carried by an event source identifier.
    /// </summary>
    /// <param name="type">The property's type.</param>
    /// <returns>True when the type is string or Guid.</returns>
    static bool IsPrimitiveKeyType(ITypeSymbol? type) => type?.SpecialType == SpecialType.System_String || type.Is("System.Guid");

    /// <summary>
    /// Reports what a query says about how it is served, which the document does not describe.
    /// </summary>
    /// <param name="method">The method exposing the query.</param>
    /// <param name="location">Where the query lives.</param>
    /// <remarks>
    /// Paging and sorting are left out of the query's shape because no caller sends them as arguments, and that is
    /// the right reading - but a query that pages is a real thing the application does, and a document that says
    /// nothing at all reads exactly like a query that does not page. Which query pages is decided the way Arc decides
    /// it, by the return type being a queryable; a paging or sorting parameter is read as well, for a signature that
    /// says it means to be served that way even though Arc fills neither in.
    /// </remarks>
    void ReportHowItIsServed(IMethodSymbol method, string location)
    {
        if (QueryReturnTypes.IsPagedByTheHost(method.ReturnType))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart,
                $"The query '{method.Name}' hands back a queryable, so the host pages and sorts it on the caller's behalf, which says how the result is asked for rather than what it is, and Screenplay has no counterpart for it",
                location);
        }

        if (method.Parameters.Any(parameter => parameter.Type.Is(WellKnownTypeNames.QueryContext)))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart,
                $"The query '{method.Name}' receives Arc's QueryContext; this is a host dependency, not a scalar context-bound argument, so no 'from $context' source was inferred",
                location);
        }

        foreach (var parameter in method.Parameters.Where(parameter => IsInput(parameter) && parameter.HasExplicitDefaultValue && parameter.ExplicitDefaultValue is not null))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableQuery,
                $"The query '{method.Name}' allows '{parameter.Name}' to be omitted; its non-null default value has no equivalent optional equality filter, so the parameter was left out",
                location);
        }

        foreach (var served in method.Parameters
            .Where(_ => _.Type.Is(WellKnownTypeNames.Paging) || _.Type.Is(WellKnownTypeNames.Sorting))
            .Select(_ => _.Type.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart,
                $"The query '{method.Name}' is served with {served.ToLowerInvariant()}, which says how the result is asked for rather than what it is, and Screenplay has no counterpart for it",
                location);
        }

        if (method.GetAttribute(WellKnownTypeNames.PathAttribute)?.GetArgument(0) is string path && !string.IsNullOrWhiteSpace(path))
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.ServingConcernWithoutCounterpart,
                $"The query '{method.Name}' is served at '{path}' rather than the conventional route, which Screenplay has no counterpart for",
                location);
        }
    }

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

        if (!collection)
        {
            return resolved;
        }

        var container = QueryReturnTypes.ContainerOf(method.ReturnType);

        if (current.NullableAnnotation == NullableAnnotation.Annotated || current is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
        {
            diagnostics.Information(
                ScreenplayDiagnosticCodes.UnmappableQuery,
                $"The query '{method.Name}' returns nullable collection elements, which query return optionality cannot state; only the collection's optionality was stated",
                location);
        }

        return resolved with { IsCollection = true, IsOptional = container.NullableAnnotation == NullableAnnotation.Annotated };
    }

    /// <summary>
    /// Converts a parameter of the query.
    /// </summary>
    /// <param name="parameter">The parameter to convert.</param>
    /// <param name="keyType">The recovered key type.</param>
    /// <returns>The <see cref="PropertyModel"/>.</returns>
    PropertyModel ToParameter(IParameterSymbol parameter, ITypeSymbol? keyType)
    {
        var type = types.ResolveCarried(keyType ?? parameter.Type);

        return new(parameter.Name, type with { IsOptional = parameter.HasExplicitDefaultValue || type.IsOptional });
    }
}
