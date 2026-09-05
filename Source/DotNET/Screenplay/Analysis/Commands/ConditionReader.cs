// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Aggregates;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Reads the condition guarding a branch of a command handler.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Screenplay has no parentheses in conditions, so a combined condition only survives a round trip when the tree is
/// left associative and shallow. Anything deeper, and anything that is not a comparison against the command's own
/// input, is reported rather than approximated.
/// <para>
/// A condition written inside a body the handler called names that body's parameters, so the bindings of the call
/// site stand in for them and the comparison is followed back to the input it really compares.
/// </para>
/// </remarks>
public class ConditionReader(ScreenplayDiagnostics diagnostics)
{
    readonly MappingSourceReader _sources = new(diagnostics);

    /// <summary>
    /// Inverts a condition, so that the other branch of a decision can be expressed.
    /// </summary>
    /// <param name="condition">The condition to invert.</param>
    /// <returns>The inverted condition, or <see langword="null"/> when it cannot be inverted.</returns>
    public static ConditionModel? Invert(ConditionModel? condition) => condition switch
    {
        ComparisonCondition comparison => comparison with { Operator = ComparisonKinds.Opposite(comparison.Operator) },
        LogicalCondition logical when Invert(logical.Left) is { } left && Invert(logical.Right) is { } right =>
            new LogicalCondition(left, !logical.IsOr, right),
        _ => null
    };

    /// <summary>
    /// Reads a condition.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read, if it is not the handler's own.</param>
    /// <returns>The condition, or <see langword="null"/> when it is not expressible.</returns>
    public ConditionModel? Read(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        string location,
        ParameterBindings? bindings = null)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return Read(parenthesized.Expression, semanticModel, owner, location, bindings);

            case PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression } negation:
                return Invert(Read(negation.Operand, semanticModel, owner, location, bindings));

            case BinaryExpressionSyntax binary when IsLogical(binary):
                return ReadLogical(binary, semanticModel, owner, location, bindings);

            case BinaryExpressionSyntax binary when ComparisonKinds.Of(binary.Kind()) is { } comparison:
                return ReadComparison(binary, comparison, semanticModel, owner, location, bindings);

            default:
                return ReadTruthy(expression, semanticModel, owner, bindings);
        }
    }

    /// <summary>
    /// Determines whether an operator combines two conditions.
    /// </summary>
    /// <param name="binary">The expression to check.</param>
    /// <returns>True when the expression combines conditions.</returns>
    static bool IsLogical(BinaryExpressionSyntax binary) =>
        binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression);

    /// <summary>
    /// Reads a bare boolean property as a comparison against true.
    /// </summary>
    /// <param name="expression">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read.</param>
    /// <returns>The condition, or <see langword="null"/> when the expression is not a boolean input.</returns>
    static ComparisonCondition? ReadTruthy(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        ParameterBindings? bindings)
    {
        if (semanticModel.GetTypeInfo(expression).Type?.SpecialType != SpecialType.System_Boolean)
        {
            return null;
        }

        var path = MappingSourceReader.ReadPath(expression, semanticModel, owner, bindings);

        return path is null ? null : new ComparisonCondition(path, ComparisonKind.Equal, new LiteralSource(true));
    }

    /// <summary>
    /// Reads two conditions combined with a logical operator.
    /// </summary>
    /// <param name="binary">The expression to read.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read.</param>
    /// <returns>The condition, or <see langword="null"/> when either side is not expressible.</returns>
    LogicalCondition? ReadLogical(
        BinaryExpressionSyntax binary,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        string location,
        ParameterBindings? bindings)
    {
        var left = Read(binary.Left, semanticModel, owner, location, bindings);
        var right = Read(binary.Right, semanticModel, owner, location, bindings);

        return left is null || right is null ? null : new LogicalCondition(left, binary.IsKind(SyntaxKind.LogicalOrExpression), right);
    }

    /// <summary>
    /// Reads a comparison between the command's own input and a value.
    /// </summary>
    /// <param name="binary">The expression to read.</param>
    /// <param name="comparison">The comparison being made.</param>
    /// <param name="semanticModel">The semantic model of the tree the expression lives in.</param>
    /// <param name="owner">The type whose properties count as the command's own input.</param>
    /// <param name="location">Where the command lives, for use in diagnostics.</param>
    /// <param name="bindings">What the call site gave the parameters of the body being read.</param>
    /// <returns>The condition, or <see langword="null"/> when either side is not expressible.</returns>
    ComparisonCondition? ReadComparison(
        BinaryExpressionSyntax binary,
        ComparisonKind comparison,
        SemanticModel semanticModel,
        ITypeSymbol owner,
        string location,
        ParameterBindings? bindings)
    {
        var left = MappingSourceReader.ReadPath(binary.Left, semanticModel, owner, bindings);
        if (left is not null)
        {
            var right = _sources.Read(binary.Right, semanticModel, owner, location, bindings);

            return right is null ? null : new ComparisonCondition(left, comparison, right);
        }

        var mirrored = MappingSourceReader.ReadPath(binary.Right, semanticModel, owner, bindings);
        if (mirrored is null)
        {
            return null;
        }

        var value = _sources.Read(binary.Left, semanticModel, owner, location, bindings);

        return value is null ? null : new ComparisonCondition(mirrored, ComparisonKinds.Mirrored(comparison), value);
    }
}
