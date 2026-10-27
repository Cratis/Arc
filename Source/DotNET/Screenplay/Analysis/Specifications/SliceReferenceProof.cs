// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Closes the inherited declaration chain over only the reference sites admitted by the slice proofs.
/// </summary>
internal static class SliceReferenceProof
{
    /// <summary>
    /// Determines whether every reference to the held slice and outcome belongs to a proven operation.
    /// </summary>
    /// <param name="steps">The complete inherited step chain.</param>
    /// <param name="models">The semantic models of the source.</param>
    /// <param name="construction">The admitted construction and its write site.</param>
    /// <param name="action">The admitted Run receiver and optional outcome write.</param>
    /// <param name="rejection">The admitted assertion receivers.</param>
    /// <returns>True only when no other held-value reference exists.</returns>
    internal static bool IsClosed(
        INamedTypeSymbol steps,
        SemanticModels models,
        SliceConstructionProof construction,
        SliceActionProof action,
        SliceRejectionProof rejection)
    {
        var allowed = construction.ReferenceSites().Concat(action.ReferenceSites()).Concat(rejection.ReferenceSites).ToList();
        return SpecificationMembers.ChainOf(steps).SelectMany(item => item.DeclaringSyntaxReferences).Select(reference => reference.GetSyntax())
            .SelectMany(declaration => declaration.DescendantNodes().OfType<ExpressionSyntax>())
            .Where(node => node is IdentifierNameSyntax or MemberAccessExpressionSyntax)
            .All(node => IsAdmitted(node, models, action, allowed));
    }

    static bool IsAdmitted(ExpressionSyntax node, SemanticModels models, SliceActionProof action, List<SyntaxNode> allowed) =>
        models.For(node.SyntaxTree) is { } model &&
        (model.GetSymbolInfo(node).Symbol is not { } symbol || !action.RefersToHeldValue(symbol) ||
         allowed.Exists(permitted => permitted.SyntaxTree == node.SyntaxTree && permitted.Span.Contains(node.Span)));
}
