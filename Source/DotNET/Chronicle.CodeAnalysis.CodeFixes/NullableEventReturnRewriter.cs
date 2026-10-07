// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
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
    /// Identifies only the validation type names generated for null return branches.
    /// </summary>
    internal static readonly SyntaxAnnotation RejectionTypeAnnotation = new();

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
        LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.NullLiteralExpression) => RewriteNull(literal),
        ConditionalExpressionSyntax conditional => conditional.WithWhenTrue(Rewrite(conditional.WhenTrue)).WithWhenFalse(Rewrite(conditional.WhenFalse)),
        ParenthesizedExpressionSyntax parenthesized => parenthesized.WithExpression(Rewrite(parenthesized.Expression)),
        _ => CheckNullability(expression)
    };

    InvocationExpressionSyntax RewriteNull(LiteralExpressionSyntax literal)
    {
        var invocation = (InvocationExpressionSyntax)ParseExpression(rejection);
        var member = (MemberAccessExpressionSyntax)invocation.Expression;
        var type = ParseName(member.Expression.ToString()).WithAdditionalAnnotations(Simplifier.Annotation, RejectionTypeAnnotation);

        return invocation.WithExpression(member.WithExpression(type)).WithTriviaFrom(literal);
    }

    ExpressionSyntax CheckNullability(ExpressionSyntax expression)
    {
        // An implicit Result conversion can accept a nullable event without a compiler error.
        // Compilation alone therefore cannot prove that a remaining event branch is non-null.
        var typeInfo = model.GetTypeInfo(expression, cancellationToken);

        // Prefer the expression's own type: a non-null event can be converted to the handler's
        // old Result<E?, ValidationResult> target without carrying a nullable branch itself.
        var type = typeInfo.Type ?? typeInfo.ConvertedType;
        if (typeInfo.Nullability.FlowState == NullableFlowState.MaybeNull ||
            (type is not null && NullableCommandEventReturnAnalyzer.NullableEvents(type, model.Compilation).Any()))
        {
            IsSafe = false;
        }

        return expression;
    }
}
