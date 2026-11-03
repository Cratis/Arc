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

    /// <summary>Reads a built-in Equals call only when its operands have equality-operator semantics.</summary>
    /// <param name="invocation">The Equals call.</param>
    /// <param name="scope">The semantic model and command whose input may be compared.</param>
    /// <param name="location">The diagnostic location.</param>
    /// <returns>The admitted comparison, or null.</returns>
    internal ComparisonCondition? ReadEquality(InvocationExpressionSyntax invocation, ProducesScope scope, string location)
    {
        var (left, right) = EqualityOperands(invocation, scope.SemanticModel);
        if (left is null || right is null)
        {
            return null;
        }
        if (!HaveMatchingBuiltInTypes(left, right, scope.SemanticModel))
        {
            return null;
        }

        return ReadOperands(left, right, ComparisonKind.Equal, new(scope.SemanticModel, scope.Command, location, scope.Bindings));
    }

    /// <summary>
    /// Determines whether an operator combines two conditions.
    /// </summary>
    /// <param name="binary">The expression to check.</param>
    /// <returns>True when the expression combines conditions.</returns>
    static bool IsLogical(BinaryExpressionSyntax binary) =>
        binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression);

    static (ExpressionSyntax? Left, ExpressionSyntax? Right) EqualityOperands(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method || method.Name != "Equals")
        {
            return default;
        }
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Any(argument => argument.NameColon is not null || argument.RefKindKeyword.RawKind != 0))
        {
            return default;
        }
        if (method.IsStatic)
        {
            return method.ContainingType.SpecialType == SpecialType.System_Object && arguments.Count == 2
                ? (arguments[0].Expression, arguments[1].Expression) : default;
        }
        return InstanceEqualityOperands(invocation, method.ContainingType);
    }

    static (ExpressionSyntax? Left, ExpressionSyntax? Right) InstanceEqualityOperands(InvocationExpressionSyntax invocation, ITypeSymbol type) =>
        HasBuiltInEquality(type) && invocation.ArgumentList.Arguments.Count == 1 && invocation.Expression is MemberAccessExpressionSyntax member
            ? (member.Expression, invocation.ArgumentList.Arguments[0].Expression) : default;

    static bool HaveMatchingBuiltInTypes(ExpressionSyntax left, ExpressionSyntax right, SemanticModel model) =>
        model.GetTypeInfo(left).Type is { } type && HasBuiltInEquality(type) && SymbolEqualityComparer.Default.Equals(type, model.GetTypeInfo(right).Type);

    /// <summary>
    /// Determines whether the casts around an operand leave its value alone, because recovering the operand path drops them.
    /// </summary>
    /// <param name="expression">The operand.</param>
    /// <param name="model">The semantic model of the tree the operand lives in.</param>
    /// <returns>True when no cast changes the value the operand compares as.</returns>
    static bool PreservesValue(ExpressionSyntax expression, SemanticModel model)
    {
        switch (expression)
        {
            case ParenthesizedExpressionSyntax parenthesized:
                return PreservesValue(parenthesized.Expression, model);

            case CheckedExpressionSyntax checkedExpression:
                return PreservesValue(checkedExpression.Expression, model);

            case CastExpressionSyntax cast:
                return PreservesCast(cast, model) && PreservesValue(cast.Expression, model);

            default:
                return true;
        }
    }

    static bool PreservesCast(CastExpressionSyntax cast, SemanticModel model)
    {
        var inner = cast.Expression;
        var constant = model.GetConstantValue(cast);
        if (constant.HasValue)
        {
            var innerConstant = model.GetConstantValue(inner);
            return innerConstant.HasValue && SameConstant(constant.Value, innerConstant.Value);
        }

        if (model.GetTypeInfo(inner).Type is not { } from || model.GetTypeInfo(cast).Type is not { } to)
        {
            return false;
        }
        var conversion = model.Compilation.ClassifyConversion(from, to);
        return conversion.IsIdentity || (to.SpecialType == SpecialType.System_Object && (conversion.IsReference || conversion.IsBoxing));
    }

    static bool SameConstant(object? converted, object? original) =>
        IsNumber(converted) && IsNumber(original)
            ? Convert.ToDecimal(converted, System.Globalization.CultureInfo.InvariantCulture) == Convert.ToDecimal(original, System.Globalization.CultureInfo.InvariantCulture)
            : Equals(converted, original);

    static bool IsNumber(object? value) => value is sbyte or byte or short or ushort or int or uint or long or ulong or decimal;

    static bool HasBuiltInEquality(ITypeSymbol type) => type.SpecialType is
        SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_String or
        SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
        SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal;

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
        if (DateConditions.Applies(binary, semanticModel))
        {
            return DateConditions.Read(binary, comparison, new(semanticModel, owner, location, bindings, diagnostics));
        }

        if (UuidConditions.Applies(binary, semanticModel, bindings))
        {
            return UuidConditions.Read(binary, comparison, new(semanticModel, location, diagnostics, (expression, model) => MappingSourceReader.ReadPath(expression, model, owner), null, bindings));
        }

        return ReadOperands(binary.Left, binary.Right, comparison, new(semanticModel, owner, location, bindings));
    }

    ComparisonCondition? ReadOperands(ExpressionSyntax leftOperand, ExpressionSyntax rightOperand, ComparisonKind comparison, ComparisonSite site)
    {
        if (!PreservesValue(leftOperand, site.Model) || !PreservesValue(rightOperand, site.Model))
        {
            return null;
        }

        var left = MappingSourceReader.ReadPath(leftOperand, site.Model, site.Owner, site.Bindings);
        if (left is not null)
        {
            var right = _sources.Read(rightOperand, site.Model, site.Owner, site.Location, site.Bindings);

            return right is null ? null : new ComparisonCondition(left, comparison, right);
        }

        var mirrored = MappingSourceReader.ReadPath(rightOperand, site.Model, site.Owner, site.Bindings);
        if (mirrored is null)
        {
            return null;
        }

        var value = _sources.Read(leftOperand, site.Model, site.Owner, site.Location, site.Bindings);

        return value is null ? null : new ComparisonCondition(mirrored, ComparisonKinds.Mirrored(comparison), value);
    }

    record ComparisonSite(SemanticModel Model, ITypeSymbol Owner, string Location, ParameterBindings? Bindings);
}
