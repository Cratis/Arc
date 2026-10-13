// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Analysis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Distinguishes results with their own destination from results that may inherit the reactor's append context.
/// </summary>
/// <param name="models">The semantic models used to resolve literal results.</param>
internal class ReactorAppendRouting(SemanticModels models)
{
    const string ExplicitEvent = "Cratis.Chronicle.EventSequences.EventForEventSourceId";

    /// <summary>
    /// Checks whether any handler may return an event using the context route.
    /// </summary>
    /// <param name="handlers">The reactor's handlers.</param>
    /// <returns>True for bare events and unproven results; false for explicit destinations, commands and no results.</returns>
    internal bool UsesContextRoute(IReadOnlyList<IMethodSymbol> handlers) => handlers.Any(UsesContextRoute);

    static bool IsOutcomeFree(ITypeSymbol type) => type.SpecialType == SpecialType.System_Void ||
        type.Is("System.Threading.Tasks.Task") || type.Is("System.Threading.Tasks.ValueTask");

    static bool IsTask(INamedTypeSymbol type) => type.OriginalDefinition.Is("System.Threading.Tasks.Task`1") ||
        type.OriginalDefinition.Is("System.Threading.Tasks.ValueTask`1");

    static bool HasKnownAlternatives(INamedTypeSymbol type) => type.IsTupleType ||
        type.OriginalDefinition.FullMetadataName().StartsWith("Cratis.Monads.Result`", StringComparison.Ordinal);

    static bool HasOwnDestination(ITypeSymbol type) => type.Is(ExplicitEvent) || CommandReader.IsCommand(type) || IsOutcomeFree(type);

    static bool HasIndependentDestination(ITypeSymbol type) => HasIndependentDestination(type, new(SymbolEqualityComparer.Default));

    static bool HasIndependentDestination(ITypeSymbol type, HashSet<ITypeSymbol> visited)
    {
        if (EventReader.IsEvent(type))
        {
            return false;
        }
        if (!visited.Add(type))
        {
            return false;
        }
        if (HasOwnDestination(type))
        {
            return true;
        }
        if (CollectionElements.ElementOf(type) is { } element)
        {
            return HasIndependentDestination(element, visited);
        }

        return type is INamedTypeSymbol named && HasIndependentAlternatives(named, visited);
    }

    static bool HasIndependentAlternatives(INamedTypeSymbol named, HashSet<ITypeSymbol> visited)
    {
        if (IsTask(named))
        {
            return HasIndependentDestination(named.TypeArguments[0], visited);
        }

        return HasKnownAlternatives(named) && named.TypeArguments.All(argument => HasIndependentDestination(argument, new(visited, SymbolEqualityComparer.Default)));
    }

    bool UsesContextRoute(IMethodSymbol handler)
    {
        if (HasIndependentDestination(handler.ReturnType))
        {
            return false;
        }
        if (HandlerBodies.Of(handler).ToList() is not [var body] || models.For(body.SyntaxTree) is not { } model)
        {
            return true;
        }
        if (ReactionReader.ReturnedBy(body, model) is not { } returned ||
            ReactionReader.ValueOf(handler, returned, model, out _, out _) is not { } value)
        {
            return true;
        }

        return !HasIndependentDestination(value, model);
    }

    bool HasIndependentDestination(ExpressionSyntax value, SemanticModel model)
    {
        if (model.GetTypeInfo(value).Type is { } type && HasIndependentDestination(type))
        {
            return true;
        }
        if (ReactionReader.ItemsOf(value, model) is { } items)
        {
            return items.All(item => HasIndependentDestination(item, model));
        }

        return false;
    }
}
