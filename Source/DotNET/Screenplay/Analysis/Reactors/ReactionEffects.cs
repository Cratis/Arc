// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Cratis.Arc.Screenplay.Analysis.Events;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Reactors;

/// <summary>
/// Reads returned constructions and their exact explicit destination wrappers.
/// </summary>
internal static class ReactionEffects
{
    /// <summary>
    /// Reads a bare construction or an exact two-argument destination wrapper.
    /// </summary>
    /// <param name="item">The returned expression.</param>
    /// <param name="model">The semantic model resolving constructions.</param>
    /// <param name="handler">The handler supplying trigger values.</param>
    /// <returns>The effect, or null when any part is code.</returns>
    internal static ReactionEffect? Read(ExpressionSyntax item, SemanticModel model, IMethodSymbol handler)
    {
        while (item is ParenthesizedExpressionSyntax parentheses)
        {
            item = parentheses.Expression;
        }
        if (item is not BaseObjectCreationExpressionSyntax creation || model.GetTypeInfo(creation).Type is not INamedTypeSymbol type)
        {
            return null;
        }
        if (!type.Is(EventWrapperType.MetadataName))
        {
            return InlineProductionShape.IsSupported(creation, model) ? new(creation, type, type, null) : null;
        }

        return ReadWrapper(creation, model, handler);
    }

    static ReactionEffect? ReadWrapper(BaseObjectCreationExpressionSyntax wrapper, SemanticModel model, IMethodSymbol handler)
    {
        if (wrapper.Initializer is not null || ExplicitEventDestination.Read(wrapper, model) is not { } envelope ||
            ReactionMappings.Destination(envelope.Destination, model, handler) is not { } destination)
        {
            return null;
        }

        return new(envelope.Event, envelope.EventType, envelope.WrapperType, destination);
    }
}
