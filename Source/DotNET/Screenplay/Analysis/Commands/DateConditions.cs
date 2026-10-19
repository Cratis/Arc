// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Admits only direct, required command inputs and deterministic literals in chronological conditions.
/// </summary>
internal static class DateConditions
{
    /// <summary>
    /// Determines whether either operand has a date primitive.
    /// </summary>
    /// <param name="binary">The comparison.</param>
    /// <param name="model">Its semantic model.</param>
    /// <returns>Whether date admission applies.</returns>
    internal static bool Applies(BinaryExpressionSyntax binary, SemanticModel model) =>
        DateComparisonOperands.Primitive(model.GetTypeInfo(binary.Left).Type) is not null ||
        DateComparisonOperands.Primitive(model.GetTypeInfo(binary.Right).Type) is not null;

    /// <summary>
    /// Reads an admitted date condition and reports unsupported operands.
    /// </summary>
    /// <param name="binary">The comparison.</param>
    /// <param name="comparison">The operator.</param>
    /// <param name="site">Where the comparison is read.</param>
    /// <returns>The condition or null.</returns>
    internal static ComparisonCondition? Read(BinaryExpressionSyntax binary, ComparisonKind comparison, DateConditionSite site)
    {
        var model = site.Model;
        var left = MappingSourceReader.ReadPath(binary.Left, model, site.Owner, site.Bindings);
        var right = MappingSourceReader.ReadPath(binary.Right, model, site.Owner, site.Bindings);
        var sources = new MappingSourceReader(site.Diagnostics);
        var operandExpression = left is not null ? binary.Right : binary.Left;
        var operand = DateOperand(operandExpression, model) ?? sources.Read(operandExpression, model, site.Owner, site.Location, site.Bindings);
        var reason = Failure(binary, model, left, right) ?? (operand is LiteralSource { Value: string } or PropertyPathSource ? null :
            DateComparisonOperands.Refusal(MappingSourceReader.Unwrap(operandExpression), model, true));
        if (reason is not null)
        {
            site.Diagnostics.Warning(ScreenplayDiagnosticCodes.UnmappableCommandProduction, $"The date comparison on '{left ?? right ?? binary.ToString()}' was left in code: {reason}", site.Location);

            return null;
        }

        if (DateComparisonOperands.Primitive(model.GetTypeInfo(binary.Left).Type) == ScreenplayPrimitive.DateTime)
        {
            new[] { left, right }.OfType<string>().ToList().ForEach(property => DateComparisonOperands.ReportInputBinding(property, site.Location, site.Diagnostics));
        }

        return left is not null ? new(left, comparison, operand!) : new(right!, ComparisonKinds.Mirrored(comparison), operand!);
    }

    static LiteralSource? DateOperand(ExpressionSyntax expression, SemanticModel model) =>
        DateComparisonOperands.Literal(expression, model) is { } date ? new LiteralSource(date) : null;

    static string? ConversionFailure(BinaryExpressionSyntax binary, SemanticModel model) =>
        ValuePreservingConversions.Preserves(binary.Left, model) && ValuePreservingConversions.Preserves(binary.Right, model)
            ? null : "user-defined operand conversions must forward the date value unchanged";

    static string? Failure(BinaryExpressionSyntax binary, SemanticModel model, string? left, string? right) =>
        ConversionFailure(binary, model) ?? PropertyFailure(binary, model, left, right);

    static string? PropertyFailure(BinaryExpressionSyntax binary, SemanticModel model, string? left, string? right)
    {
        var first = model.GetTypeInfo(binary.Left).Type;
        var second = model.GetTypeInfo(binary.Right).Type;
        if (DateComparisonOperands.Primitive(first) != DateComparisonOperands.Primitive(second))
        {
            return "Date and DateTime operands cannot be mixed";
        }
        if (DateComparisonOperands.IsOptional(first) || DateComparisonOperands.IsOptional(second))
        {
            return "optional operands are not admitted in command conditions";
        }

        return (left is null && right is null) || left?.Contains('.', StringComparison.Ordinal) == true || right?.Contains('.', StringComparison.Ordinal) == true
            ? "date conditions require a direct command property, not a context, concept or read path" : null;
    }
}
