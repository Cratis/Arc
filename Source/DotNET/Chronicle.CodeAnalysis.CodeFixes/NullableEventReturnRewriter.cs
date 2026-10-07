// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Cratis.Arc.Chronicle.CodeAnalysis.CodeFixes;

/// <summary>
/// Rewrites only handler return expressions, leaving nested functions and unrelated null values alone.
/// </summary>
/// <param name="rejection">The expression used for a null result.</param>
/// <param name="model">The original method's semantic model.</param>
/// <param name="cancellationToken">The cancellation token.</param>
sealed class NullableEventReturnRewriter(string rejection, SemanticModel model, CancellationToken cancellationToken) : CSharpSyntaxRewriter
{
    /// <summary>
    /// Gets whether every unmodified return expression is known not to be null.
    /// </summary>
    public bool IsSafe { get; private set; } = true;

    /// <inheritdoc/>
    public override SyntaxNode? VisitReturnStatement(ReturnStatementSyntax node) =>
        node.Expression is { } expression ? node.WithExpression(Rewrite(expression)) : node;

    /// <inheritdoc/>
    public override SyntaxNode? VisitArrowExpressionClause(ArrowExpressionClauseSyntax node) => node.WithExpression(Rewrite(node.Expression));

    /// <inheritdoc/>
    public override SyntaxNode? VisitLocalFunctionStatement(LocalFunctionStatementSyntax node) => node;

    /// <inheritdoc/>
    public override SyntaxNode? VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => node;

    /// <inheritdoc/>
    public override SyntaxNode? VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => node;

    /// <inheritdoc/>
    public override SyntaxNode? VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => node;

    ExpressionSyntax Rewrite(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NullLiteralExpression) => ParseExpression(rejection).WithTriviaFrom(literal),
        ConditionalExpressionSyntax conditional => conditional.WithWhenTrue(Rewrite(conditional.WhenTrue)).WithWhenFalse(Rewrite(conditional.WhenFalse)),
        ParenthesizedExpressionSyntax parenthesized => parenthesized.WithExpression(Rewrite(parenthesized.Expression)),
        _ => CheckNullability(expression)
    };

    ExpressionSyntax CheckNullability(ExpressionSyntax expression)
    {
        // An implicit Result conversion can accept a nullable event without a compiler error.
        // Compilation alone therefore cannot prove that a remaining event branch is non-null.
        if (model.GetTypeInfo(expression, cancellationToken).Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            IsSafe = false;
        }

        return expression;
    }
}
