// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Binds the held slice's exact construction to its directly constructed command.
/// </summary>
/// <param name="construction">The stable construction and its semantic model.</param>
/// <param name="commandCreation">The directly constructed command argument.</param>
/// <param name="command">The exact command type.</param>
internal sealed class SliceConstructionProof(HeldConstruction construction, BaseObjectCreationExpressionSyntax commandCreation, INamedTypeSymbol command)
{
    /// <summary>
    /// Gets the directly constructed command argument.
    /// </summary>
    internal BaseObjectCreationExpressionSyntax CommandCreation => commandCreation;

    /// <summary>
    /// Gets the exact constructed command type.
    /// </summary>
    internal INamedTypeSymbol Command => command;

    /// <summary>
    /// Gets the semantic model of the held construction.
    /// </summary>
    internal SemanticModel Model => construction.SemanticModel;

    /// <summary>
    /// Gets the command type of the first held shipped slice in the inherited chain.
    /// </summary>
    /// <param name="type">The type whose held members are inspected.</param>
    /// <returns>The held slice's command type, or null when no slice is held.</returns>
    internal static ITypeSymbol? CommandOf(INamedTypeSymbol type) =>
        SpecificationMembers.ChainOf(type).SelectMany(item => item.GetMembers()).Select(TypeOf).OfType<INamedTypeSymbol>()
            .FirstOrDefault(heldType => heldType.Is(SliceActionProof.SliceType))?.TypeArguments.SingleOrDefault();

    /// <summary>
    /// Reads the shipped single-command constructor without any outer initializer.
    /// </summary>
    /// <param name="steps">The specification's inherited steps.</param>
    /// <param name="action">The execution whose slice is constructed.</param>
    /// <param name="held">The held-value construction reader.</param>
    /// <returns>The construction binding, or null when its command cannot be proved.</returns>
    internal static SliceConstructionProof? TryRead(INamedTypeSymbol steps, SliceActionProof action, HeldValues held)
    {
        if (held.ConstructionOf(action.Receiver, action.Model) is not { } construction ||
            construction.Creation.Initializer is not null ||
            construction.SemanticModel.GetSymbolInfo(construction.Creation).Symbol is not IMethodSymbol constructor ||
            !IsSliceConstructor(constructor, action.Command) ||
            construction.Creation.ArgumentList?.Arguments is not [var argument] ||
            argument.Expression is not BaseObjectCreationExpressionSyntax commandCreation ||
            construction.SemanticModel.GetTypeInfo(commandCreation).Type is not INamedTypeSymbol command ||
            !SymbolEqualityComparer.Default.Equals(command, action.Command) ||
            !SymbolEqualityComparer.Default.Equals(command, SpecificationMembers.CommandOf(steps)) || !CommandReader.IsCommand(command))
        {
            return null;
        }

        return new(construction, commandCreation, command);
    }

    /// <summary>
    /// Determines whether setup consists only of the held slice construction.
    /// </summary>
    /// <param name="steps">The inherited step chain to inspect.</param>
    /// <param name="action">The action identifying the held slice field.</param>
    /// <param name="models">The semantic models of the source.</param>
    /// <returns>True when the construction is the only setup and no explicit constructor is declared.</returns>
    internal bool HasOnlyConstructionSetup(INamedTypeSymbol steps, SliceActionProof action, SemanticModels models)
    {
        var bodies = SpecificationMembers.MethodsIn(steps, SpecificationMembers.EstablishMethod).SelectMany(HandlerBodies.Of).ToList();
        return (IsFieldInitializer(action.Slice, models) || bodies.Exists(body => IsConstructionAssignment(body, action.Slice, models))) &&
            bodies.TrueForAll(body => body is BlockSyntax { Statements.Count: 0 } || IsConstructionAssignment(body, action.Slice, models)) &&
            SpecificationMembers.ChainOf(steps).SelectMany(item => item.InstanceConstructors)
                .All(constructor => constructor.IsImplicitlyDeclared || constructor.DeclaringSyntaxReferences.Length == 0);
    }

    /// <summary>
    /// Gets the admitted construction assignment target, when construction occurs in setup.
    /// </summary>
    /// <returns>The exact construction write site, or no sites for a field initializer.</returns>
    internal IEnumerable<SyntaxNode> ReferenceSites() => construction.Creation.Parent is AssignmentExpressionSyntax setup ? [setup.Left] : [];

    static bool IsSliceConstructor(IMethodSymbol constructor, ITypeSymbol command) =>
        constructor.MethodKind == MethodKind.Constructor && constructor.ContainingType.Is(SliceActionProof.SliceType) &&
        constructor.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, command);

    static ITypeSymbol? TypeOf(ISymbol member) => member switch
    {
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        _ => null
    };

    bool IsFieldInitializer(ISymbol slice, SemanticModels models) =>
        construction.Creation.Parent is EqualsValueClauseSyntax
        {
            Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } } field
        } && models.For(construction.Creation.SyntaxTree) is { } model &&
        SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(field), slice);

    bool IsConstructionAssignment(SyntaxNode body, ISymbol slice, SemanticModels models) =>
        SliceActionProof.ExpressionOf(body) is AssignmentExpressionSyntax assignment && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
        assignment.Right.SyntaxTree == construction.Creation.SyntaxTree && assignment.Right.Span == construction.Creation.Span && SliceActionProof.IsDirect(assignment.Left) &&
        models.For(assignment.SyntaxTree) is { } model && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left).Symbol, slice);
}
