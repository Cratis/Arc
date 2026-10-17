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
    static readonly Dictionary<SyntaxKind, (ComparisonKind Direct, ComparisonKind Inverted)> _kinds = new()
    {
        [SyntaxKind.EqualsExpression] = (ComparisonKind.Equal, ComparisonKind.NotEqual),
        [SyntaxKind.NotEqualsExpression] = (ComparisonKind.NotEqual, ComparisonKind.Equal),
        [SyntaxKind.LessThanExpression] = (ComparisonKind.LessThan, ComparisonKind.GreaterThanOrEqual),
        [SyntaxKind.LessThanOrEqualExpression] = (ComparisonKind.LessThanOrEqual, ComparisonKind.GreaterThan),
        [SyntaxKind.GreaterThanExpression] = (ComparisonKind.GreaterThan, ComparisonKind.LessThanOrEqual),
        [SyntaxKind.GreaterThanOrEqualExpression] = (ComparisonKind.GreaterThanOrEqual, ComparisonKind.LessThan)
    };

    /// <summary>
    /// Reads a guard condition as a comparison over command input or a read.
    /// </summary>
    /// <param name="expression">The guard condition.</param>
    /// <param name="model">The semantic model of the guard.</param>
    /// <param name="scope">The command being read.</param>
    /// <param name="invert">Whether the guard rejects when the condition holds.</param>
    /// <returns>The comparison, or null when the guard is not one the model states.</returns>
    /// <remarks>
    /// A condition is a binary comparison, <c>Object.Equals(a, b)</c>, or the negation (<c>!</c>) of either, which is how
    /// Stage renders a requirement it guards: <c>if (!(!Object.Equals(read.Value, ""))) return ...</c>.
    /// </remarks>
    internal ComparisonCondition? Condition(ExpressionSyntax expression, SemanticModel model, ReadScope scope, bool invert)
    {
        if (Shape(expression, model, invert) is not { } shape)
        {
            return null;
        }

        var site = new DateConditionSite(model, scope.Command, scope.Location, null, diagnostics);

        if (shape.Binary is { } uuid && UuidConditions.Applies(uuid, model))
        {
            return UuidCondition(uuid, shape.Invert, model, scope);
        }

        return shape.Binary is { } binary && DateConditions.Applies(binary, model)
            ? DateCondition(binary, site, shape.Invert)
            : ReadComparison(shape, scope.Sources, site);
    }

    static ConditionShape? Shape(ExpressionSyntax expression, SemanticModel model, bool invert) => MappingSourceReader.Unwrap(expression) switch
    {
        PrefixUnaryExpressionSyntax negation when negation.IsKind(SyntaxKind.LogicalNotExpression) => Shape(negation.Operand, model, !invert),
        BinaryExpressionSyntax binary => new(binary.Left, binary.Right, binary.Kind(), invert, binary),
        InvocationExpressionSyntax { ArgumentList.Arguments: [var left, var right] } call when IsObjectEquals(call, model) =>
            new(left.Expression, right.Expression, SyntaxKind.EqualsExpression, invert, null),
        _ => null
    };

    static bool IsObjectEquals(InvocationExpressionSyntax call, SemanticModel model) =>
        model.GetSymbolInfo(call).Symbol is IMethodSymbol { IsStatic: true, ContainingType.SpecialType: SpecialType.System_Object } method && string.Equals(method.Name, nameof(object.Equals), StringComparison.Ordinal);

    static bool IsPlainComparison(ConditionShape shape, SemanticModel model) =>
        (shape.Binary is null || model.GetOperation(shape.Binary) is IBinaryOperation { OperatorMethod: null, LeftOperand: not IConversionOperation, RightOperand: not IConversionOperation }) &&
        SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(shape.Left).Type, model.GetTypeInfo(shape.Right).Type);

    static bool IsRefusedEnumeration(ExpressionSyntax right, SemanticModel model)
    {
        var constant = model.GetConstantValue(right);

        return EnumConstants.EnumerationOf(right, model) is { } enumeration && (!constant.HasValue || !EnumConstants.TryResolve(enumeration, constant.Value, out _));
    }

    static ComparisonKind? KindOf(SyntaxKind kind, bool invert) => _kinds.TryGetValue(kind, out var pair) ? (invert ? pair.Inverted : pair.Direct) : null;

    static bool IsStated(ConditionShape shape, SemanticModel model) =>
        KindOf(shape.Kind, shape.Invert) is not null && IsPlainComparison(shape, model) && !IsRefusedEnumeration(shape.Right, model);

    /// <summary>
    /// Reads the path an operand refers to, noting whether it is a bare name that is neither command input nor a read property, which the model does not state.
    /// </summary>
    /// <param name="operand">The operand.</param>
    /// <param name="sources">The proven source paths.</param>
    /// <param name="model">The semantic model of the operand.</param>
    /// <param name="command">The command being read.</param>
    /// <returns>The path, or null when the operand is not one.</returns>
    static ConditionPath? Operand(ExpressionSyntax operand, AuthoringSources sources, SemanticModel model, INamedTypeSymbol command)
    {
        var input = MappingSourceReader.ReadPath(operand, model, command);

        return (input ?? sources.ReadPath(operand, model)) is { } path ? new(path, input is null && !path.Contains('.', StringComparison.Ordinal)) : null;
    }

    ComparisonCondition? UuidCondition(BinaryExpressionSyntax binary, bool invert, SemanticModel model, ReadScope scope)
    {
        var site = new UuidConditionSite(
            model,
            scope.Location,
            diagnostics,
            (path, operandModel) => MappingSourceReader.ReadPath(path, operandModel, scope.Command),
            (path, operandModel) => scope.Sources.ReadPath(path, operandModel) is { } read && read.Contains('.', StringComparison.Ordinal) ? read : null);
        var condition = ComparisonKinds.Of(binary.Kind()) is { } kind ? UuidConditions.Read(binary, kind, site) : null;

        return invert && condition is not null ? condition with { Operator = ComparisonKinds.Opposite(condition.Operator) } : condition;
    }

    ComparisonCondition? DateCondition(BinaryExpressionSyntax binary, DateConditionSite site, bool invert)
    {
        var condition = new ConditionReader(diagnostics).Read(binary, site.Model, (INamedTypeSymbol)site.Owner, site.Location);

        return (invert ? ConditionReader.Invert(condition) : condition) as ComparisonCondition;
    }

    ComparisonCondition? ReadComparison(ConditionShape shape, AuthoringSources sources, DateConditionSite site)
    {
        var model = site.Model;
        var command = (INamedTypeSymbol)site.Owner;
        if (!IsStated(shape, model))
        {
            return null;
        }

        var left = Operand(shape.Left, sources, model, command);
        var right = Operand(shape.Right, sources, model, command);
        if (left is not { IsBare: false } || right is { IsBare: true })
        {
            return null;
        }

        var operand = right is null ? new MappingSourceReader(diagnostics).Read(shape.Right, model, command, site.Location) : new PropertyPathSource(right.Value);

        return operand is null ? null : new(left.Value, KindOf(shape.Kind, shape.Invert)!.Value, operand);
    }

    sealed record ConditionShape(ExpressionSyntax Left, ExpressionSyntax Right, SyntaxKind Kind, bool Invert, BinaryExpressionSyntax? Binary);

    sealed record ConditionPath(string Value, bool IsBare);
}
