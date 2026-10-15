// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Events;

/// <summary>
/// Carries the semantically bound arguments of an explicit event destination wrapper.
/// </summary>
/// <param name="Wrapper">The returned wrapper.</param>
/// <param name="Destination">The argument bound to the destination parameter.</param>
/// <param name="Event">The directly constructed event payload.</param>
/// <param name="EventType">The exact event type.</param>
/// <param name="WrapperType">The exact wrapper type.</param>
internal record ExplicitEventDestination(
    BaseObjectCreationExpressionSyntax Wrapper,
    ExpressionSyntax Destination,
    BaseObjectCreationExpressionSyntax Event,
    INamedTypeSymbol EventType,
    INamedTypeSymbol WrapperType)
{
    /// <summary>
    /// Proves the exact two-argument wrapper and its directly constructed event, independently of argument spelling.
    /// </summary>
    /// <param name="wrapper">The candidate construction.</param>
    /// <param name="model">The model binding constructor parameters and payload types.</param>
    /// <returns>The bound envelope, or null for unsupported constructions.</returns>
    internal static ExplicitEventDestination? Read(BaseObjectCreationExpressionSyntax wrapper, SemanticModel model)
    {
        if (wrapper.ArgumentList?.Arguments.Count != 2 ||
            model.GetOperation(wrapper) is not IObjectCreationOperation { Type: INamedTypeSymbol type } operation ||
            !type.Is("Cratis.Chronicle.EventSequences.EventForEventSourceId") ||
            ArgumentAt(operation, 0) is not { } destination ||
            ArgumentAt(operation, 1) is not { } payload ||
            payload is not BaseObjectCreationExpressionSyntax creation ||
            model.GetTypeInfo(creation).Type is not INamedTypeSymbol eventType ||
            !EventReader.IsEvent(eventType) || !InlineProductionShape.IsSupported(creation, model))
        {
            return null;
        }

        return new(wrapper, destination, creation, eventType, type);
    }

    static ExpressionSyntax? ArgumentAt(IObjectCreationOperation operation, int ordinal) =>
        (operation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == ordinal)?.Syntax as ArgumentSyntax)?.Expression;
}
