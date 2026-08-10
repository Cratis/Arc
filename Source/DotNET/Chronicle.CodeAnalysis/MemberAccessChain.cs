// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reads a chain of member accesses — <c>a.B.C(...)</c> — one link at a time.
/// </summary>
/// <remarks>
/// A rule that cares what is done to a value has to follow the chain the value sits in, and C# spells that chain
/// three ways: a plain member access, a member binding under <c>?.</c>, and either of those wrapped in
/// parentheses or a null-forgiving <c>!</c>. This type hides that difference so a rule can ask what it means to
/// ask — what comes next, what is this link called, what is it applied to.
/// </remarks>
static class MemberAccessChain
{
    /// <summary>
    /// Gets the next link applied to an expression.
    /// </summary>
    /// <param name="expression">The expression to read forward from.</param>
    /// <returns>The next member access or member binding, or null when the expression is the end of the chain.</returns>
    public static ExpressionSyntax? Next(ExpressionSyntax expression) => expression.Parent switch
    {
        MemberAccessExpressionSyntax member when member.Expression == expression => member,
        ConditionalAccessExpressionSyntax conditional when conditional.Expression == expression => LeadingBindingOf(conditional.WhenNotNull),
        _ => null
    };

    /// <summary>
    /// Gets the name a link accesses.
    /// </summary>
    /// <param name="expression">The member access or member binding to read.</param>
    /// <returns>The accessed name, or null when the expression is neither.</returns>
    public static SimpleNameSyntax? NameOf(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax member => member.Name,
        MemberBindingExpressionSyntax binding => binding.Name,
        _ => null
    };

    /// <summary>
    /// Gets the expression a link is applied to.
    /// </summary>
    /// <param name="expression">The member access, member binding, or invocation of either.</param>
    /// <returns>The receiver, or null when there is none to read.</returns>
    /// <remarks>
    /// Under <c>?.</c> the receiver is not a child of the link — it belongs to the enclosing conditional access —
    /// so it is fetched from there.
    /// </remarks>
    public static ExpressionSyntax? ReceiverOf(ExpressionSyntax expression) => AccessorOf(expression) switch
    {
        MemberAccessExpressionSyntax member => Unwrap(member.Expression),
        MemberBindingExpressionSyntax binding => ConditionalReceiverOf(binding),
        _ => null
    };

    /// <summary>
    /// Renders an access as source text on a single line, with its receiver restored when it is under a <c>?.</c>.
    /// </summary>
    /// <param name="expression">The access to render.</param>
    /// <returns>The source text of the access, with its own line breaks and indentation removed.</returns>
    /// <remarks>
    /// A fluent call spread over several lines carries those line breaks and their indentation into its source
    /// text, and a diagnostic message is rendered on one line by the CLI, by SARIF readers, and by every IDE
    /// error list.
    /// </remarks>
    public static string Describe(ExpressionSyntax expression) =>
        AccessorOf(expression) is MemberBindingExpressionSyntax binding && ConditionalReceiverOf(binding) is { } receiver
            ? $"{OnOneLine(receiver)}?{OnOneLine(expression)}"
            : OnOneLine(expression);

    static ExpressionSyntax AccessorOf(ExpressionSyntax expression) =>
        expression is InvocationExpressionSyntax invocation ? invocation.Expression : expression;

    static ExpressionSyntax? ConditionalReceiverOf(MemberBindingExpressionSyntax binding) =>
        binding.Ancestors().OfType<ConditionalAccessExpressionSyntax>().FirstOrDefault() is { } conditional
            ? Unwrap(conditional.Expression)
            : null;

    static MemberBindingExpressionSyntax? LeadingBindingOf(ExpressionSyntax expression) => expression switch
    {
        MemberBindingExpressionSyntax binding => binding,
        MemberAccessExpressionSyntax member => LeadingBindingOf(member.Expression),
        InvocationExpressionSyntax invocation => LeadingBindingOf(invocation.Expression),
        ConditionalAccessExpressionSyntax conditional => LeadingBindingOf(conditional.Expression),
        _ => null
    };

    static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression) => Unwrap(postfix.Operand),
        _ => expression
    };

    static string OnOneLine(SyntaxNode node) =>
        node.NormalizeWhitespace(indentation: string.Empty, eol: " ").ToString();
}
