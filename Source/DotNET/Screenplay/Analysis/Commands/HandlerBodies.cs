// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Resolves the bodies of a handler and the event types its signature promises.
/// </summary>
/// <remarks>
/// A handler living in a referenced package has metadata but no body, which is the one case where source analysis
/// degrades to what reflection could see - the events the signature names, with no mappings.
/// </remarks>
public static class HandlerBodies
{
    /// <summary>
    /// Gets the body of every declaration of a method.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <returns>The bodies, empty when the method has no source.</returns>
    public static IEnumerable<SyntaxNode> Of(IMethodSymbol method) =>
        method.DeclaringSyntaxReferences
            .Select(_ => _.GetSyntax())
            .Select(BodyOf)
            .OfType<SyntaxNode>();

    /// <summary>
    /// Gets every event type named anywhere within a type, walking wrappers, tuples and collections.
    /// </summary>
    /// <param name="type">The type to walk.</param>
    /// <returns>The event types, ordered by name.</returns>
    public static IEnumerable<INamedTypeSymbol> EventTypesIn(ITypeSymbol? type)
    {
        var found = new List<INamedTypeSymbol>();
        Collect(type, found, new(SymbolEqualityComparer.Default));

        return found.OrderBy(_ => _.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Determines whether a return type yields the identifier of an event source alongside the event.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a tuple carrying an event.</returns>
    public static bool YieldsEventSourceId(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol named)
        {
            return false;
        }

        if (named.IsTupleType)
        {
            return named.TupleElements.Any(_ => EventReader.IsEvent(_.Type)) && named.TupleElements.Length > 1;
        }

        return named.TypeArguments.Any(YieldsEventSourceId);
    }

    /// <summary>
    /// Gets the body of a declaration.
    /// </summary>
    /// <param name="node">The declaration to read.</param>
    /// <returns>The body, or <see langword="null"/> when the declaration has none.</returns>
    static SyntaxNode? BodyOf(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax method => (SyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
        LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody?.Expression,
        _ => null
    };

    /// <summary>
    /// Collects the event types named within a type.
    /// </summary>
    /// <param name="type">The type to walk.</param>
    /// <param name="found">The event types found so far.</param>
    /// <param name="visited">The types already walked, guarding against a recursive shape.</param>
    static void Collect(ITypeSymbol? type, List<INamedTypeSymbol> found, HashSet<ITypeSymbol> visited)
    {
        if (type is null || !visited.Add(type))
        {
            return;
        }

        if (type is IArrayTypeSymbol array)
        {
            Collect(array.ElementType, found, visited);
            return;
        }

        if (type is not INamedTypeSymbol named)
        {
            return;
        }

        if (EventReader.IsEvent(named))
        {
            found.Add(named);
            return;
        }

        foreach (var argument in named.IsTupleType ? named.TupleElements.Select(_ => _.Type) : named.TypeArguments)
        {
            Collect(argument, found, visited);
        }
    }
}
