// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Screenplay.Analysis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Applies model-bound selection while retaining Screenplay's conventional query shapes.
/// </summary>
internal static class ModelBoundQuerySelection
{
    /// <summary>
    /// Recognizes directly declared read models and inherited explicit selection.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>Whether it is a read model.</returns>
    internal static bool IsReadModel(ITypeSymbol type) => type.HasAttribute(WellKnownTypeNames.ReadModelAttribute) ||
        (type is INamedTypeSymbol named && ModelBoundQueryMethodSymbols.RequiresQueryAttribute(named));

    /// <summary>
    /// Validates query intent even on types that owner filtering would discard.
    /// </summary>
    /// <param name="type">The catalogued type.</param>
    internal static void Validate(INamedTypeSymbol type) => ModelBoundQueryMethodSymbols.EnsureValid(type);

    /// <summary>
    /// Validates intent before selecting and ordering query methods.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <returns>The selected methods.</returns>
    internal static IEnumerable<IMethodSymbol> MethodsOf(INamedTypeSymbol type)
    {
        Validate(type);
        return ModelBoundQueryMethodSymbols.GetMethods(type, method => IsConventionalQueryFor(method, type))
            .OrderBy(_ => _.ToDisplayString(), StringComparer.Ordinal);
    }

    static bool IsConventionalQueryFor(IMethodSymbol method, INamedTypeSymbol owner) =>
        method is { MethodKind: MethodKind.Ordinary, IsStatic: true, ReturnsVoid: false, TypeParameters.Length: 0 } &&
        ReturnsModel(method, owner);

    static bool ReturnsModel(IMethodSymbol method, INamedTypeSymbol owner)
    {
        var collection = false;
        return SymbolEqualityComparer.Default.Equals(UnderlyingTypes.Of(QueryReturnTypes.Unwrap(method.ReturnType, ref collection)), owner);
    }
}
