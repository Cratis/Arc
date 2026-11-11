// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Events;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

public partial class CommandAuthoringReader
{
    /// <summary>Proves that a handler has no fact-recording or external effects.</summary>
    /// <param name="handlers">The handlers of the command.</param>
    /// <param name="authoring">The recovered pure response.</param>
    /// <returns>Whether the command has no provisioning method and every body is empty or consists only of the recovered response and generated values.</returns>
    public bool HasNoFactBehavior(IReadOnlyList<IMethodSymbol> handlers, CommandAuthoringModel? authoring) =>
        handlers.Count > 0 && handlers.All(handler => HandlerHasNoFactBehavior(handler, authoring));

    /// <summary>
    /// Determines whether a value of a type can be, or can carry, an event the pipeline appends.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="visited">The types already checked.</param>
    /// <param name="allowOpen">Whether open types such as <see cref="object"/> and interfaces are accepted, for subexpressions.</param>
    /// <returns>True for events, tuples, collections, results and unions with an event, and for types that could hold one at runtime.</returns>
    /// <remarks>
    /// A handler appends whatever event its returned value holds, whether directly, in a tuple, in a collection, or
    /// in a <c>Result</c> or <c>OneOf</c> branch. A returned <see cref="object"/>, <see langword="dynamic"/>, interface or type
    /// parameter can hold an event at runtime and is not proven free of one.
    /// </remarks>
    static bool MayCarryEvent(ITypeSymbol type, HashSet<ITypeSymbol> visited, bool allowOpen = false)
    {
        if (!visited.Add(type))
        {
            return false;
        }

        if (EventReader.IsEvent(type))
        {
            return true;
        }

        if (!allowOpen && IsOpenType(type))
        {
            return true;
        }

        return MayCarryEventInside(type, visited, allowOpen);
    }

    static bool IsOpenType(ITypeSymbol type) =>
        type.SpecialType == SpecialType.System_Object || type.TypeKind is TypeKind.Dynamic or TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Error;

    static bool MayCarryEventInside(ITypeSymbol type, HashSet<ITypeSymbol> visited, bool allowOpen) => type switch
    {
        IArrayTypeSymbol array => MayCarryEvent(array.ElementType, visited, allowOpen),
        INamedTypeSymbol { IsTupleType: true } tuple => tuple.TupleElements.Any(element => MayCarryEvent(element.Type, visited, allowOpen)),
        INamedTypeSymbol named => named.TypeArguments.Any(argument => MayCarryEvent(argument, visited, allowOpen)),
        _ => false
    };

    static bool StatesNoResponse(CommandAuthoringModel authoring) => authoring is not { Response: not null } and not { ResponseFields.Count: > 0 };

    static bool IsSingleReturnBlock(BlockSyntax block) =>
        block.Statements.LastOrDefault() is ReturnStatementSyntax &&
        block.Statements.All(statement => statement is ReturnStatementSyntax or LocalDeclarationStatementSyntax) &&
        block.Statements.OfType<ReturnStatementSyntax>().Count() == 1;

    static bool HasProvide(INamedTypeSymbol command)
    {
        for (var current = command; current is not null; current = current.BaseType)
        {
            // Runtime discovery includes instance methods, but not private methods inherited from a base type.
            if (current.GetMembers("Provide").OfType<IMethodSymbol>().Any(method => IsDiscoverableProvide(method, current, command)))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsDiscoverableProvide(IMethodSymbol method, INamedTypeSymbol declaring, INamedTypeSymbol command) =>
        !method.IsStatic && (SymbolEqualityComparer.Default.Equals(declaring, command) || method.DeclaredAccessibility != Accessibility.Private);

    bool HandlerHasNoFactBehavior(IMethodSymbol handler, CommandAuthoringModel? authoring)
    {
        if (HasProvide(handler.ContainingType) || HandlerBodies.Of(handler).ToArray() is not [var body])
        {
            return false;
        }

        if (body is BlockSyntax { Statements.Count: 0 })
        {
            return true;
        }

        return authoring is not null && !CarriesEffects(handler, authoring, body) && (body is ExpressionSyntax || IsPureGeneratedBlock(body, authoring));
    }

    bool CarriesEffects(IMethodSymbol handler, CommandAuthoringModel authoring, SyntaxNode body) =>
        ContainsOperation(handler.ReturnType) || StatesNoResponse(authoring) ||
        MayCarryEvent(handler.ReturnType, new(SymbolEqualityComparer.Default)) ||
        HasEventCarryingExpression(body);

    bool HasEventCarryingExpression(SyntaxNode body) =>
        models.For(body.SyntaxTree) is not { } bodyModel ||
        body.DescendantNodesAndSelf().OfType<ExpressionSyntax>().Any(expression =>
            bodyModel.GetTypeInfo(expression).Type is { } type && MayCarryEvent(type, new(SymbolEqualityComparer.Default), allowOpen: true));

    bool IsPureGeneratedBlock(SyntaxNode body, CommandAuthoringModel authoring) =>
        body is BlockSyntax block && IsSingleReturnBlock(block) &&
        block.Statements.OfType<LocalDeclarationStatementSyntax>().SelectMany(statement => statement.Declaration.Variables).All(variable => IsGeneratedLocal(variable, authoring));

    bool IsGeneratedLocal(VariableDeclaratorSyntax variable, CommandAuthoringModel authoring) =>
        models.For(variable.SyntaxTree) is { } model && model.GetDeclaredSymbol(variable) is ILocalSymbol local &&
        authoring.Generated.Any(property => property.Name == local.Name);
}
