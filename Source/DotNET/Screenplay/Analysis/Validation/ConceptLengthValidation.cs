// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Validation;

/// <summary>
/// Reads the null-tolerant inline predicates a concept validator states for its underlying value.
/// </summary>
/// <remarks>
/// A rule that passes an absent value and otherwise holds the length to a lower bound is a minimum length, which is
/// what the declarative rule means. Any other shape is left in code: the predicate is matched, never approximated.
/// </remarks>
internal static class ConceptLengthValidation
{
    /// <summary>
    /// Reads the checksum or minimum length a concept validator states for its underlying value.
    /// </summary>
    /// <param name="call">The Must call.</param>
    /// <param name="model">The model resolving its predicate.</param>
    /// <returns>The rule on the value, or null when the predicate is neither.</returns>
    internal static ValidationRuleModel? ReadRule(InvocationExpressionSyntax call, SemanticModel model)
    {
        if (ChecksumValidation.Read(call, model) is { } checksum)
        {
            return new("Value", ValidationRuleKind.Checksum, checksum, null);
        }

        return ReadMinimum(call, model) is { } minimum ? new("Value", ValidationRuleKind.Min, minimum, null) : null;
    }

    /// <summary>
    /// Reads <c>value is null || value.Length &gt;= bound</c> as a minimum length.
    /// </summary>
    /// <param name="call">The Must call.</param>
    /// <param name="model">The model resolving its predicate.</param>
    /// <returns>The inclusive minimum length, or null.</returns>
    internal static int? ReadMinimum(InvocationExpressionSyntax call, SemanticModel model)
    {
        if (!ChecksumValidation.IsStringConcept(call, model) || call.ArgumentList.Arguments is not [{ Expression: SimpleLambdaExpressionSyntax lambda }])
        {
            return null;
        }
        if (Unguarded(lambda, model) is not { } rest || model.GetOperation(rest) is not IBinaryOperation { OperatorKind: BinaryOperatorKind.GreaterThanOrEqual } comparison)
        {
            return null;
        }

        return IsLengthOf(Unconverted(comparison.LeftOperand), model.GetDeclaredSymbol(lambda.Parameter)) ? WholeNumber(comparison.RightOperand) : null;
    }

    /// <summary>
    /// Gets what a lambda requires of a value that is present, when it passes an absent value.
    /// </summary>
    /// <param name="lambda">The predicate.</param>
    /// <param name="model">The model resolving the lambda's parameter.</param>
    /// <returns>The right-hand side of <c>value is null ||</c>, or null when the lambda does not open with that guard.</returns>
    internal static ExpressionSyntax? Unguarded(SimpleLambdaExpressionSyntax lambda, SemanticModel model) =>
        lambda.ExpressionBody is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalOrExpression } guard &&
        guard.Left is IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } }, Expression: var subject } &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(subject).Symbol, model.GetDeclaredSymbol(lambda.Parameter))
            ? guard.Right
            : null;

    static bool IsLengthOf(IOperation operand, ISymbol? parameter) =>
        operand is IPropertyReferenceOperation { Property: { ContainingType.SpecialType: SpecialType.System_String } property, Instance: IParameterReferenceOperation reference } &&
        string.Equals(property.Name, "Length", StringComparison.Ordinal) && SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);

    static IOperation Unconverted(IOperation operand) => operand is IConversionOperation { IsImplicit: true } conversion ? conversion.Operand : operand;

    static int? WholeNumber(IOperation operand)
    {
        if (!operand.ConstantValue.HasValue || operand.ConstantValue.Value is not (int or long or short or byte or decimal))
        {
            return null;
        }
        var bound = Convert.ToDecimal(operand.ConstantValue.Value, System.Globalization.CultureInfo.InvariantCulture);

        return bound >= 0 && bound <= int.MaxValue && decimal.Truncate(bound) == bound ? (int)bound : null;
    }
}
