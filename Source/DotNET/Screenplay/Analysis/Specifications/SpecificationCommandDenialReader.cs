// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Withholds gate-only denial scenarios when visible command code can supply a different denial.
/// </summary>
/// <param name="models">The semantic models owning the command's source declarations.</param>
internal sealed class SpecificationCommandDenialReader(SemanticModels models)
{
    /// <summary>
    /// Checks the exact issued command, including inherited members and visible result-bearing expressions.
    /// </summary>
    /// <param name="draft">The recovered scenario containing the command's exact source evidence.</param>
    /// <returns>Why the gate cannot explain the denial alone, or null when no code denial is visible.</returns>
    internal string? Failure(SpecificationDraft draft)
    {
        if (draft.When is not { } action || !draft.GetStateEvidence().TryGetValue(action, out var evidence) ||
            evidence.Artifact is not INamedTypeSymbol command || command.DeclaringSyntaxReferences.Length == 0)
        {
            return "the command code is unavailable, so authorization denial outside the stated gate cannot be ruled out";
        }

        return CommandTypes(command).Any(CanDeny)
            ? "the command code can return an authorization denial; denial is not explained by the stated gate alone"
            : null;
    }

    static IEnumerable<INamedTypeSymbol> CommandTypes(INamedTypeSymbol command)
    {
        for (var current = command; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            yield return current;
        }
    }

    static bool CanCarryDenial(ITypeSymbol? type) => type switch
    {
        { SpecialType: SpecialType.System_Object } or { TypeKind: TypeKind.Dynamic or TypeKind.TypeParameter } => true,
        IArrayTypeSymbol array => CanCarryDenial(array.ElementType),
        INamedTypeSymbol named => named.Is("Cratis.Arc.Authorization.AuthorizationResult") ||
            named.FindBase("Cratis.Arc.Authorization.AuthorizationResult") is not null ||
            named.Is("Cratis.Arc.Commands.CommandResult") || named.FindBase("Cratis.Arc.Commands.CommandResult") is not null ||
            named.TypeArguments.Any(CanCarryDenial),
        _ => false
    };

    static bool CanReturnDenial(ITypeSymbol type) => CanCarryDenial(type) || type switch
    {
        { TypeKind: TypeKind.Interface } => true,
        IArrayTypeSymbol array => CanReturnDenial(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Any(CanReturnDenial),
        _ => false
    };

    bool CanDeny(INamedTypeSymbol command) =>
        command.GetMembers().OfType<IMethodSymbol>().Any(method => !method.IsImplicitlyDeclared && CanReturnDenial(method.ReturnType)) ||
        command.DeclaringSyntaxReferences.Any(reference => SourceCanDeny(reference.GetSyntax()));

    bool SourceCanDeny(SyntaxNode source)
    {
        if (models.For(source.SyntaxTree) is not { } model)
        {
            return true;
        }

        return source.DescendantNodes().OfType<ExpressionSyntax>().Any(expression => CanCarryDenial(model.GetTypeInfo(expression).Type));
    }
}
