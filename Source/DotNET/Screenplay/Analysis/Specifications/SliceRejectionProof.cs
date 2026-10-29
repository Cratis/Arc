// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Proves every assertion is the same slice's unnamed rejection and retains its exact receiver sites.
/// </summary>
/// <param name="evidence">The first assertion in specification assertion order.</param>
/// <param name="referenceSites">The validated held-value references in all assertions.</param>
internal sealed class SliceRejectionProof(Location evidence, IReadOnlyList<SyntaxNode> referenceSites)
{
    /// <summary>
    /// Gets the first rejection assertion's evidence location.
    /// </summary>
    internal Location Evidence => evidence;

    /// <summary>
    /// Gets every validated held-value assertion receiver site.
    /// </summary>
    internal IReadOnlyList<SyntaxNode> ReferenceSites => referenceSites;

    /// <summary>
    /// Reads a nonempty set of exact parameterless shipped rejection assertions.
    /// </summary>
    /// <param name="type">The type whose inherited assertions are inspected.</param>
    /// <param name="action">The slice execution and its optional outcome.</param>
    /// <param name="models">The semantic models of the source.</param>
    /// <returns>The complete assertion proof, or null when any assertion is unsupported.</returns>
    internal static SliceRejectionProof? TryRead(INamedTypeSymbol type, SliceActionProof action, SemanticModels models)
    {
        var assertions = SpecificationMembers.AssertionsIn(type).SelectMany(HandlerBodies.Of).ToList();
        if (assertions.Count == 0)
        {
            return null;
        }

        var references = assertions.ConvertAll(assertion => RejectionReceiver(assertion, action, models));
        return references.Exists(reference => reference is null) ? null : new(assertions[0].GetLocation(), references.OfType<SyntaxNode>().ToList());
    }

    static ExpressionSyntax? RejectionReceiver(SyntaxNode body, SliceActionProof action, SemanticModels models)
    {
        if (SliceActionProof.ExpressionOf(body) is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax receiver, ArgumentList.Arguments.Count: 0 } assertion ||
            models.For(body.SyntaxTree) is not { } model || model.GetSymbolInfo(assertion).Symbol is not IMethodSymbol method ||
            !IsRejection(method, action.Command))
        {
            return null;
        }

        if (action.Outcome is { } outcome && SliceActionProof.IsDirect(receiver.Expression) &&
            SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver.Expression).Symbol, outcome))
        {
            return receiver.Expression;
        }

        return ThenReceiver(receiver.Expression, action.Slice, model);
    }

    static bool IsRejection(IMethodSymbol method, ITypeSymbol command) =>
        string.Equals(method.Name, "ShouldBeRejected", StringComparison.Ordinal) &&
        method is { IsStatic: false, Arity: 0, Parameters.Length: 0, ReturnsVoid: true } &&
        method.ContainingType.Is(SliceActionProof.OutcomeType) && SymbolEqualityComparer.Default.Equals(method.ContainingType.TypeArguments[0], command);

    static ExpressionSyntax? ThenReceiver(ExpressionSyntax receiver, ISymbol slice, SemanticModel model) =>
        receiver is MemberAccessExpressionSyntax then &&
        model.GetSymbolInfo(then).Symbol is IPropertySymbol { IsStatic: false } property &&
        string.Equals(property.Name, "Then", StringComparison.Ordinal) && property.ContainingType.Is(SliceActionProof.SliceType) &&
        SliceActionProof.IsDirect(then.Expression) && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(then.Expression).Symbol, slice)
            ? then.Expression : null;
}
