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
/// A guard is an <see langword="if"/> without an <see langword="else"/> that returns <c>ValidationResult.Error</c>, directly or
/// wrapped in a <c>OneOf</c> or <c>Result</c> factory (<c>FromT1</c>, for instance), with any message. The message is kept
/// only when it is a constant: an absence guard does not carry it, and a requirement needs it.
/// Only the guards before the first other statement count: they run before anything is produced, which is the order
/// the model checks absence and requirements in.
/// </remarks>
internal static class ProtectedReadGuards
{
    static readonly string[] _factoryTypes = ["OneOf", "Result"];

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
    /// Finds the <c>ValidationResult.Error</c> call a returned expression carries, looking through <c>OneOf</c> and <c>Result</c> factories.
    /// </summary>
    /// <param name="expression">The returned expression.</param>
    /// <param name="model">The semantic model of the expression.</param>
    /// <returns>The call, or null when the expression does not return a validation error.</returns>
    internal static InvocationExpressionSyntax? ErrorCall(ExpressionSyntax expression, SemanticModel model) =>
        WithoutCasts(expression) is InvocationExpressionSyntax invocation && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method ? Call(invocation, method, model) : null;

    /// <summary>
    /// Reads the constant message of a <c>ValidationResult.Error</c> call.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <param name="model">The semantic model of the call.</param>
    /// <returns>The message, or null when it is computed.</returns>
    internal static string? MessageOf(InvocationExpressionSyntax call, SemanticModel model) =>
        call.ArgumentList.Arguments is [var argument] && model.GetConstantValue(argument.Expression) is { HasValue: true, Value: string message } ? message : null;

    /// <summary>
    /// Reads the constant message of a returned validation error.
    /// </summary>
    /// <param name="expression">The returned expression.</param>
    /// <param name="model">The semantic model of the expression.</param>
    /// <returns>The message, or null when the expression is not a validation error with a constant message.</returns>
    internal static string? ErrorMessage(ExpressionSyntax expression, SemanticModel model) => ErrorCall(expression, model) is { } call ? MessageOf(call, model) : null;

    static InvocationExpressionSyntax? Call(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel model) => IsValidationError(method)
        ? invocation
        : IsResultFactory(method) && invocation.ArgumentList.Arguments is [var wrapped] ? ErrorCall(wrapped.Expression, model) : null;

    static ExpressionSyntax WithoutCasts(ExpressionSyntax expression) => MappingSourceReader.Unwrap(expression) is CastExpressionSyntax cast ? WithoutCasts(cast.Expression) : MappingSourceReader.Unwrap(expression);

    static bool IsValidationError(IMethodSymbol method) => method.Name == "Error" && method.ContainingType.Is("Cratis.Arc.Validation.ValidationResult");

    static bool IsResultFactory(IMethodSymbol method) => method.IsStatic && _factoryTypes.Contains(method.ContainingType.OriginalDefinition.Name);

    static ReadGuard? Guard(StatementSyntax statement, SemanticModel model)
    {
        if (statement is not IfStatementSyntax { Else: null } guard)
        {
            return null;
        }

        var returned = guard.Statement is BlockSyntax { Statements: [ReturnStatementSyntax error] } ? error : guard.Statement as ReturnStatementSyntax;

        return returned?.Expression is { } expression && ErrorCall(expression, model) is { } call ? new(guard, MessageOf(call, model), model) : null;
    }

    static bool IsNull(ExpressionSyntax expression) => MappingSourceReader.Unwrap(expression).IsKind(SyntaxKind.NullLiteralExpression);
}
