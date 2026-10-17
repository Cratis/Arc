// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the comparisons a rejecting guard over a read states.
/// </summary>
/// <param name="diagnostics">The diagnostic sink.</param>
internal sealed class ReadConditions(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Reads a guard condition as a comparison over command input or a read.
    /// </summary>
    /// <param name="expression">The guard condition.</param>
    /// <param name="model">The semantic model of the guard.</param>
    /// <param name="scope">The command being read.</param>
    /// <param name="invert">Whether the guard rejects when the condition holds.</param>
    /// <returns>The comparison, or null when the guard is not one the model states.</returns>
    internal ComparisonCondition? Condition(ExpressionSyntax expression, SemanticModel model, ReadScope scope, bool invert)
    {
        var site = new DateConditionSite(model, scope.Command, scope.Location, null, diagnostics);

        return MappingSourceReader.Unwrap(expression) is BinaryExpressionSyntax binary && DateConditions.Applies(binary, model)
            ? DateCondition(binary, site, invert)
            : ReadComparison(expression, scope.Sources, site, invert);
    }

    ComparisonCondition? DateCondition(BinaryExpressionSyntax binary, DateConditionSite site, bool invert)
    {
        var condition = new ConditionReader(diagnostics).Read(binary, site.Model, (INamedTypeSymbol)site.Owner, site.Location);

        return (invert ? ConditionReader.Invert(condition) : condition) as ComparisonCondition;
    }

    ComparisonCondition? ReadComparison(ExpressionSyntax expression, AuthoringSources sources, DateConditionSite site, bool invert)
    {
        var model = site.Model;
        var command = (INamedTypeSymbol)site.Owner;
        var location = site.Location;
        if (MappingSourceReader.Unwrap(expression) is not BinaryExpressionSyntax comparison ||
            model.GetOperation(comparison) is not IBinaryOperation { OperatorMethod: null } operation ||
            operation.LeftOperand is IConversionOperation || operation.RightOperand is IConversionOperation ||
            !SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(comparison.Left).Type, model.GetTypeInfo(comparison.Right).Type))
        {
            return null;
        }

        var left = MappingSourceReader.ReadPath(comparison.Left, model, command) ?? sources.ReadPath(comparison.Left, model);
        var right = MappingSourceReader.ReadPath(comparison.Right, model, command) ?? sources.ReadPath(comparison.Right, model);
        if ((left?.Contains('.', StringComparison.Ordinal) == false && MappingSourceReader.ReadPath(comparison.Left, model, command) is null) ||
            (right?.Contains('.', StringComparison.Ordinal) == false && MappingSourceReader.ReadPath(comparison.Right, model, command) is null))
        {
            return null;
        }

        var constant = model.GetConstantValue(comparison.Right);
        if (EnumConstants.EnumerationOf(comparison.Right, model) is { } enumeration &&
            (!constant.HasValue || !EnumConstants.TryResolve(enumeration, constant.Value, out _)))
        {
            return null;
        }

        var operand = right is not null ? new PropertyPathSource(right) : new MappingSourceReader(diagnostics).Read(comparison.Right, model, command, location);
        var kind = comparison.Kind() switch
        {
            SyntaxKind.EqualsExpression => invert ? ComparisonKind.NotEqual : ComparisonKind.Equal,
            SyntaxKind.NotEqualsExpression => invert ? ComparisonKind.Equal : ComparisonKind.NotEqual,
            SyntaxKind.LessThanExpression => invert ? ComparisonKind.GreaterThanOrEqual : ComparisonKind.LessThan,
            SyntaxKind.LessThanOrEqualExpression => invert ? ComparisonKind.GreaterThan : ComparisonKind.LessThanOrEqual,
            SyntaxKind.GreaterThanExpression => invert ? ComparisonKind.LessThanOrEqual : ComparisonKind.GreaterThan,
            SyntaxKind.GreaterThanOrEqualExpression => invert ? ComparisonKind.LessThan : ComparisonKind.GreaterThanOrEqual,
            _ => (ComparisonKind?)null
        };

        return left is not null && kind is { } op && operand is not null
            ? new(left, op, operand)
            : null;
    }
}
