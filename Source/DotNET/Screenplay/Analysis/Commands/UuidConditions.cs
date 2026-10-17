// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Admits Uuid equality in command conditions as decision 0090 defines it, and reports what it refuses.
/// </summary>
internal static class UuidConditions
{
    /// <summary>
    /// Determines whether either operand is a Uuid.
    /// </summary>
    /// <param name="binary">The comparison.</param>
    /// <param name="model">Its semantic model.</param>
    /// <param name="bindings">The call-site bindings, if any.</param>
    /// <returns>Whether Uuid admission applies.</returns>
    internal static bool Applies(BinaryExpressionSyntax binary, SemanticModel model, ParameterBindings? bindings = null) =>
        UuidComparisonOperands.IsUuid(model.GetTypeInfo(binary.Left).Type) || UuidComparisonOperands.IsUuid(model.GetTypeInfo(binary.Right).Type) ||
        UuidComparisonOperands.IsUuid(UuidBoundOperand.Resolve(binary.Left, model, bindings)?.Type) || UuidComparisonOperands.IsUuid(UuidBoundOperand.Resolve(binary.Right, model, bindings)?.Type);

    /// <summary>
    /// Reads an admitted Uuid condition and reports a refused one.
    /// </summary>
    /// <param name="binary">The comparison.</param>
    /// <param name="comparison">The operator.</param>
    /// <param name="site">Where the comparison is read.</param>
    /// <returns>The condition with the property or read path on the left, or null.</returns>
    internal static ComparisonCondition? Read(BinaryExpressionSyntax binary, ComparisonKind comparison, UuidConditionSite site)
    {
        var boundLeft = UuidBoundOperand.Resolve(binary.Left, site.Model, site.Bindings);
        var boundRight = UuidBoundOperand.Resolve(binary.Right, site.Model, site.Bindings);
        var left = Operand(boundLeft?.Value ?? new(binary.Left, site.Model), site);
        var right = Operand(boundRight?.Value ?? new(binary.Right, site.Model), site);
        var reason = Failure(boundLeft, boundRight, comparison) ?? Shape(left, right);
        if (reason is not null)
        {
            site.Diagnostics.Warning(ScreenplayDiagnosticCodes.UnmappableCommandProduction, $"The Uuid comparison on '{Named(left, right, binary)}' was left in code: {reason}", site.Location);

            return null;
        }

        return left.Path is not null ? new(left.Path, comparison, right.Source!) : new(right.Path!, comparison, left.Source!);
    }

    static (string? Path, MappingSourceModel? Source, ExpressionSyntax Expression) Operand(BoundArgument argument, UuidConditionSite site)
    {
        var (expression, model) = argument;
        var path = site.CommandPath(expression, model) is { } command && !command.Contains('.', StringComparison.Ordinal) ? command : site.ReadPath?.Invoke(expression, model);

        return path is not null
            ? (path, new PropertyPathSource(path), expression)
            : (null, UuidComparisonOperands.Literal(MappingSourceReader.Unwrap(expression), model) is { } text ? new LiteralSource(text) : null, expression);
    }

    static string? Failure(UuidBoundOperand? left, UuidBoundOperand? right, ComparisonKind comparison)
    {
        if (left is null || right is null)
        {
            return "the operand's parameter binding is cyclic or exceeds the supported depth";
        }

        return comparison is not (ComparisonKind.Equal or ComparisonKind.NotEqual)
            ? "only == and != are admitted on Uuid values, not ordering"
            : OperandFailure(left, right) ?? ConceptFailure(left.Type, right.Type);
    }

    static string? OperandFailure(UuidBoundOperand left, UuidBoundOperand right)
    {
        if (left.HasNonTransparentCast || right.HasNonTransparentCast)
        {
            return "casts that change the operand type are not admitted in Uuid conditions";
        }
        if (!UuidComparisonOperands.IsUuid(left.Type) || !UuidComparisonOperands.IsUuid(right.Type))
        {
            return "a Uuid cannot be compared with a value of another type";
        }
        if (left.IsOptional || right.IsOptional)
        {
            return "optional operands are not admitted in command conditions";
        }

        return null;
    }

    static string? ConceptFailure(ITypeSymbol? first, ITypeSymbol? second) =>
        UuidComparisonOperands.Concept(first) is { } a && UuidComparisonOperands.Concept(second) is { } b && !SymbolEqualityComparer.Default.Equals(a, b)
            ? "different Uuid concepts or identities cannot be compared; compare values of the same concept" : null;

    static string? Shape(
        (string? Path, MappingSourceModel? Source, ExpressionSyntax Expression) left,
        (string? Path, MappingSourceModel? Source, ExpressionSyntax Expression) right)
    {
        if (left.Path is null && right.Path is null)
        {
            return "a comparison needs a command property or protected read path on one side";
        }

        var other = left.Path is null ? left : right;

        return other.Source is null ? UuidComparisonOperands.Refusal(MappingSourceReader.Unwrap(other.Expression)) : null;
    }

    static string Named(
        (string? Path, MappingSourceModel? Source, ExpressionSyntax Expression) left,
        (string? Path, MappingSourceModel? Source, ExpressionSyntax Expression) right,
        BinaryExpressionSyntax binary) => left.Path ?? right.Path ?? binary.ToString();
}
