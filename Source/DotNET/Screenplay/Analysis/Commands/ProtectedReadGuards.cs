// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the rejecting guards a Handle body opens with, which state what a protected read must satisfy.
/// </summary>
/// <remarks>
/// A guard is an <see langword="if"/> without an <see langword="else"/> that returns <c>ValidationResult.Error</c> with a literal message.
/// Only the guards before the first other statement count: they run before anything is produced, which is the order
/// the model checks absence and requirements in.
/// </remarks>
internal static class ProtectedReadGuards
{
    /// <summary>
    /// Reads the guards a method body opens with.
    /// </summary>
    /// <param name="method">The Handle or Provide method.</param>
    /// <param name="models">The source models.</param>
    /// <returns>The leading guards in source order.</returns>
    internal static IReadOnlyList<ReadGuard> Leading(IMethodSymbol method, SemanticModels models)
    {
        var guards = new List<ReadGuard>();
        if (HandlerBodies.Of(method).ToArray() is not [BlockSyntax block] || models.For(block.SyntaxTree) is not { } model)
        {
            return guards;
        }

        foreach (var statement in block.Statements)
        {
            if (Guard(statement, model) is not { } guard)
            {
                break;
            }

            guards.Add(guard);
        }

        return guards;
    }

    /// <summary>
    /// Finds the parameter an absence check tests.
    /// </summary>
    /// <param name="condition">The guard condition.</param>
    /// <param name="model">The semantic model of the body.</param>
    /// <returns>The tested symbol, or null when the condition is not a plain null check.</returns>
    internal static ISymbol? AbsenceOf(ExpressionSyntax condition, SemanticModel model)
    {
        var tested = MappingSourceReader.Unwrap(condition) switch
        {
            IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax constant } pattern when IsNull(constant.Expression) => pattern.Expression,
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) && IsNull(binary.Right) => binary.Left,
            BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) && IsNull(binary.Left) => binary.Right,
            _ => null
        };

        return tested is null ? null : model.GetSymbolInfo(MappingSourceReader.Unwrap(tested)).Symbol;
    }

    /// <summary>
    /// Reads the literal message of a <c>ValidationResult.Error</c> call.
    /// </summary>
    /// <param name="expression">The expression.</param>
    /// <param name="model">The semantic model of the expression.</param>
    /// <returns>The message, or null when the expression is not such a call.</returns>
    internal static string? ErrorMessage(ExpressionSyntax expression, SemanticModel model) => expression is InvocationExpressionSyntax { ArgumentList.Arguments: [var argument] } invocation &&
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && method.Name == "Error" && method.ContainingType.Is("Cratis.Arc.Validation.ValidationResult") &&
        model.GetConstantValue(argument.Expression) is { HasValue: true, Value: string message } ? message : null;

    static ReadGuard? Guard(StatementSyntax statement, SemanticModel model)
    {
        if (statement is not IfStatementSyntax { Else: null } guard)
        {
            return null;
        }

        var returned = guard.Statement is BlockSyntax { Statements: [ReturnStatementSyntax error] } ? error : guard.Statement as ReturnStatementSyntax;

        return returned?.Expression is { } expression && ErrorMessage(expression, model) is { } message ? new(guard, message, model) : null;
    }

    static bool IsNull(ExpressionSyntax expression) => MappingSourceReader.Unwrap(expression).IsKind(SyntaxKind.NullLiteralExpression);
}
