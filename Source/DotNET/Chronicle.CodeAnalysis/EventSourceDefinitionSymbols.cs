// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reads event source names and declared streams without instantiating definitions.
/// </summary>
internal static class EventSourceDefinitionSymbols
{
    /// <summary>
    /// The generic command definition declaration.
    /// </summary>
    internal const string DeclarationAttribute = "Cratis.Arc.Chronicle.Commands.EventSourceAttribute`1";
    const string DefinitionAttribute = "Cratis.Chronicle.EventSources.EventSourceAttribute";
    const string StreamAttribute = "Cratis.Chronicle.EventSources.EventStreamAttribute";
    const string DefinitionSuffix = "EventSource";

    /// <summary>
    /// Collects unambiguously named definitions from the current compilation.
    /// </summary>
    /// <param name="root">The compilation's global namespace.</param>
    /// <returns>Definitions by source name.</returns>
    internal static ImmutableDictionary<string, INamedTypeSymbol> DefinitionsIn(INamespaceSymbol root)
    {
        var definitions = TypesIn(root).Select(type => (Type: type, Name: NameOf(type))).Where(_ => _.Name is not null);

        // Duplicate names are rejected by Chronicle; suggesting either definition would guess.
        return definitions.GroupBy(_ => _.Name!, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToImmutableDictionary(group => group.Key, group => group.Single().Type, StringComparer.Ordinal);
    }

    /// <summary>
    /// Reads the explicit or convention-based definition name.
    /// </summary>
    /// <param name="type">The candidate definition.</param>
    /// <returns>The name, or null for a type without a definition attribute.</returns>
    internal static string? NameOf(INamedTypeSymbol type)
    {
        var attribute = Find(type, DefinitionAttribute);
        if (attribute is null)
        {
            return null;
        }

        if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string explicitName)
        {
            return explicitName;
        }

        return type.Name.Length > DefinitionSuffix.Length && type.Name.EndsWith(DefinitionSuffix, StringComparison.Ordinal)
            ? type.Name.Substring(0, type.Name.Length - DefinitionSuffix.Length)
            : type.Name;
    }

    /// <summary>
    /// Checks a definition's own stream declarations, including referenced definitions.
    /// </summary>
    /// <param name="definition">The definition type.</param>
    /// <param name="stream">The stream name.</param>
    /// <returns>Whether the stream is declared.</returns>
    internal static bool DeclaresStream(INamedTypeSymbol definition, string stream) =>
        definition.GetAttributes().Any(attribute =>
            FullName(attribute.AttributeClass) == StreamAttribute &&
            attribute.ConstructorArguments.Length > 0 &&
            (attribute.ConstructorArguments[0].Value as string) == stream);

    /// <summary>
    /// Finds an attribute by metadata name.
    /// </summary>
    /// <param name="symbol">The attributed symbol.</param>
    /// <param name="name">The attribute metadata name.</param>
    /// <returns>The matching attribute, if any.</returns>
    internal static AttributeData? Find(ISymbol symbol, string name) =>
        symbol.GetAttributes().FirstOrDefault(attribute => FullName(attribute.AttributeClass) == name);

    /// <summary>
    /// Gets the namespace-qualified metadata name of a type.
    /// </summary>
    /// <param name="type">The type symbol.</param>
    /// <returns>The metadata name, or an empty string for an unresolved type.</returns>
    internal static string FullName(INamedTypeSymbol? type) => type is null ? string.Empty : $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}";

    static IEnumerable<INamedTypeSymbol> TypesIn(INamespaceOrTypeSymbol root)
    {
        foreach (var member in root.GetMembers().OfType<INamespaceOrTypeSymbol>())
        {
            if (member is INamedTypeSymbol type)
            {
                yield return type;
            }

            foreach (var nested in TypesIn(member))
            {
                yield return nested;
            }
        }
    }
}
