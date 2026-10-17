// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <returns>Whether Uuid admission applies.</returns>
    internal static bool Applies(BinaryExpressionSyntax binary, SemanticModel model) =>
        UuidComparisonOperands.IsUuid(model.GetTypeInfo(binary.Left).Type) || UuidComparisonOperands.IsUuid(model.GetTypeInfo(binary.Right).Type);

    /// <summary>
    /// Reads an admitted Uuid condition and reports a refused one.
    /// </summary>
    /// <param name="binary">The comparison.</param>
    /// <param name="comparison">The operator.</param>
    /// <param name="site">Where the comparison is read.</param>
    /// <returns>The condition with the property or read path on the left, or null.</returns>
    internal static ComparisonCondition? Read(BinaryExpressionSyntax binary, ComparisonKind comparison, UuidConditionSite site)
    {
        var left = Operand(binary.Left, site);
        var right = Operand(binary.Right, site);
        var reason = Failure(binary, comparison, site.Model) ?? Shape(left, right);
        if (reason is not null)
        {
            site.Diagnostics.Warning(ScreenplayDiagnosticCodes.UnmappableCommandProduction, $"The Uuid comparison on '{Named(left, right, binary)}' was left in code: {reason}", site.Location);

            return null;
        }

        return left.Path is not null ? new(left.Path, comparison, right.Source!) : new(right.Path!, comparison, left.Source!);
    }

    static (string? Path, MappingSourceModel? Source, ExpressionSyntax Expression) Operand(ExpressionSyntax expression, UuidConditionSite site)
    {
        var path = site.CommandPath(expression) is { } command && !command.Contains('.', StringComparison.Ordinal) ? command : site.ReadPath?.Invoke(expression);

        return path is not null
            ? (path, new PropertyPathSource(path), expression)
            : (null, UuidComparisonOperands.Literal(MappingSourceReader.Unwrap(expression), site.Model) is { } text ? new LiteralSource(text) : null, expression);
    }

    static string? Failure(BinaryExpressionSyntax binary, ComparisonKind comparison, SemanticModel model)
    {
        var first = model.GetTypeInfo(binary.Left).Type;
        var second = model.GetTypeInfo(binary.Right).Type;
        if (comparison is not (ComparisonKind.Equal or ComparisonKind.NotEqual))
        {
            return "only == and != are admitted on Uuid values, not ordering";
        }
        if (HasNonTransparentCast(binary.Left, model) || HasNonTransparentCast(binary.Right, model))
        {
            return "casts that change the operand type are not admitted in Uuid conditions";
        }
        if (!UuidComparisonOperands.IsUuid(first) || !UuidComparisonOperands.IsUuid(second))
        {
            return "a Uuid cannot be compared with a value of another type";
        }
        if (UuidComparisonOperands.IsOptional(first) || UuidComparisonOperands.IsOptional(second))
        {
            return "optional operands are not admitted in command conditions";
        }

        return UuidComparisonOperands.Concept(first) is { } a && UuidComparisonOperands.Concept(second) is { } b && !SymbolEqualityComparer.Default.Equals(a, b)
            ? "different Uuid concepts or identities cannot be compared; compare values of the same concept" : null;
    }

    static bool HasNonTransparentCast(ExpressionSyntax expression, SemanticModel model) => expression.DescendantNodesAndSelf()
        .OfType<CastExpressionSyntax>()
        .Any(cast => !SymbolEqualityComparer.IncludeNullability.Equals(model.GetTypeInfo(cast.Expression).Type, model.GetTypeInfo(cast).Type));

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
