// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>Proves that a tuple response cannot replace an event source identity.</summary>
internal static class TupleResponseIdentity
{
    /// <summary>Checks the response type without treating opaque union branches as proof.</summary>
    /// <param name="type">The bound response type.</param>
    /// <returns>Whether the response cannot carry an identity.</returns>
    internal static bool CannotOverrideIdentity(ITypeSymbol type) => CannotOverrideIdentity(type, new(SymbolEqualityComparer.Default));

    static bool CannotOverrideIdentity(ITypeSymbol type, HashSet<ITypeSymbol> visited)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }
        if (!visited.Add(type) || IsOpaque(named))
        {
            return false;
        }

        for (var current = named; current is not null; current = current.BaseType)
        {
            if (IsIdentity(current))
            {
                return false;
            }

            if (IsUnion(current))
            {
                return current.TypeArguments.Length > 0 && current.TypeArguments.All(branch =>
                    CannotOverrideIdentity(branch, new HashSet<ITypeSymbol>(visited, SymbolEqualityComparer.Default)));
            }
        }

        // An arbitrary IOneOf implementation need not expose its possible branches as type arguments.
        return !named.AllInterfaces.Any(contract => contract.Is("OneOf.IOneOf"));
    }

    static bool IsIdentity(INamedTypeSymbol type) =>
        type.Is("Cratis.Chronicle.Events.EventSourceId") || type.Is("Cratis.Chronicle.Events.EventSourceId`1");

    static bool IsOpaque(INamedTypeSymbol type) => type.TypeKind is TypeKind.Interface or TypeKind.Error ||
        type.SpecialType == SpecialType.System_Object || type.IsTupleType || type.Is(WellKnownTypeNames.ConceptAs);

    static bool IsUnion(INamedTypeSymbol type)
    {
        var namespaceName = type.ContainingNamespace.ToDisplayString();
        return (string.Equals(namespaceName, "OneOf", StringComparison.Ordinal) &&
                (string.Equals(type.Name, "OneOf", StringComparison.Ordinal) || string.Equals(type.Name, "OneOfBase", StringComparison.Ordinal))) ||
            (string.Equals(namespaceName, "Cratis.Monads", StringComparison.Ordinal) && string.Equals(type.Name, "Result", StringComparison.Ordinal));
    }
}
