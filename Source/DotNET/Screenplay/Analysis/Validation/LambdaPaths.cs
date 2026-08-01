// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Reads the property a lambda selects.
/// </summary>
/// <remarks>
/// Validators, projections and constraints all name a property the same way - a lambda that walks into its
/// argument. Reading it from the syntax is exact, where reading it from the compiled expression tree at runtime
/// loses everything the lambda did not compile down to a member access.
/// </remarks>
public static class LambdaPaths
{
    /// <summary>
    /// Reads the dotted path a lambda selects.
    /// </summary>
    /// <param name="expression">The lambda to read.</param>
    /// <returns>The path, or <see langword="null"/> when the lambda does not simply select a property.</returns>
    public static string? Read(ExpressionSyntax? expression)
    {
        var (parameter, body) = expression switch
        {
            SimpleLambdaExpressionSyntax simple => (simple.Parameter.Identifier.ValueText, simple.ExpressionBody),
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } parenthesized =>
                (parenthesized.ParameterList.Parameters[0].Identifier.ValueText, parenthesized.ExpressionBody),
            _ => (null, null)
        };

        return parameter is null || body is null ? null : ReadFrom(body, parameter);
    }

    /// <summary>
    /// Reads the dotted path an expression walks from a named root.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="root">The name of the root the path starts at.</param>
    /// <returns>The path, or <see langword="null"/> when the expression is not a walk from the root.</returns>
    static string? ReadFrom(ExpressionSyntax expression, string root)
    {
        var segments = new List<string>();
        var current = Unwrap(expression);

        while (current is MemberAccessExpressionSyntax member)
        {
            segments.Insert(0, member.Name.Identifier.ValueText);
            current = Unwrap(member.Expression);
        }

        return current is IdentifierNameSyntax identifier &&
            string.Equals(identifier.Identifier.ValueText, root, StringComparison.Ordinal) &&
            segments.Count > 0
                ? string.Join('.', segments)
                : null;
    }

    /// <summary>
    /// Strips the wrappers that do not change what an expression selects.
    /// </summary>
    /// <param name="expression">The expression to strip.</param>
    /// <returns>The wrapped expression.</returns>
    static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        CastExpressionSyntax cast => Unwrap(cast.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.SuppressNullableWarningExpression } suppress =>
            Unwrap(suppress.Operand),
        _ => expression
    };
}
