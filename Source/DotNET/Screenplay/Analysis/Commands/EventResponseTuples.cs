// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Distinguishes an ordinary tuple response from a response that can override the command's event source.
/// </summary>
public static class EventResponseTuples
{
    /// <summary>
    /// Determines whether a returned event and response keep the command context's event source.
    /// </summary>
    /// <param name="type">The handler's return type, or the constructed tuple type.</param>
    /// <returns>Whether the tuple carries one event and a response that cannot be an event source identifier.</returns>
    public static bool UsesCommandContext(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.Is("System.Threading.Tasks.Task`1") || named.Is("System.Threading.Tasks.ValueTask`1"))
        {
            return UsesCommandContext(named.TypeArguments[0]);
        }

        if (!named.IsTupleType || named.TupleElements.Length != 2)
        {
            return false;
        }

        var events = named.TupleElements.Where(element => EventReader.IsEvent(element.Type)).ToArray();
        var responses = named.TupleElements.Where(element => !EventReader.IsEvent(element.Type)).ToArray();

        return events.Length == 1 && responses is [var response] && CannotOverrideIdentity(response.Type);
    }

    static bool CannotOverrideIdentity(ITypeSymbol type) => CannotOverrideIdentity(type, new(SymbolEqualityComparer.Default));

    static bool CannotOverrideIdentity(ITypeSymbol type, HashSet<ITypeSymbol> visited)
    {
        if (!visited.Add(type) || type is not INamedTypeSymbol named || named.TypeKind is TypeKind.Interface or TypeKind.Error ||
            named.SpecialType == SpecialType.System_Object || named.IsTupleType || named.Is(WellKnownTypeNames.ConceptAs))
        {
            return false;
        }

        for (var current = named; current is not null; current = current.BaseType)
        {
            if (current.Is("Cratis.Chronicle.Events.EventSourceId") || current.Is("Cratis.Chronicle.Events.EventSourceId`1"))
            {
                return false;
            }

            var namespaceName = current.ContainingNamespace.ToDisplayString();
            if ((string.Equals(namespaceName, "OneOf", StringComparison.Ordinal) &&
                 (string.Equals(current.Name, "OneOf", StringComparison.Ordinal) || string.Equals(current.Name, "OneOfBase", StringComparison.Ordinal))) ||
                (string.Equals(namespaceName, "Cratis.Monads", StringComparison.Ordinal) && string.Equals(current.Name, "Result", StringComparison.Ordinal)))
            {
                return current.TypeArguments.Length > 0 && current.TypeArguments.All(branch =>
                    CannotOverrideIdentity(branch, new HashSet<ITypeSymbol>(visited, SymbolEqualityComparer.Default)));
            }
        }

        // An arbitrary IOneOf implementation need not expose its possible branches as type arguments.
        return !named.AllInterfaces.Any(contract => contract.Is("OneOf.IOneOf"));
    }
}
