// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Queries;

/// <summary>
/// Proves one direct property equality, optionally skipped for a null caller parameter.
/// </summary>
/// <param name="model">The semantic model resolving both sides of the comparison.</param>
/// <param name="parameter">The caller parameter being proved.</param>
internal sealed class EqualityQueryFilter(SemanticModel model, IParameterSymbol parameter)
{
    IParameterSymbol? _item;

    /// <summary>
    /// Gets whether null skips the proved comparison.
    /// </summary>
    internal bool Optional { get; private set; }

    /// <summary>
    /// Proves one same-property equality in a predicate.
    /// </summary>
    /// <param name="lambda">The query predicate.</param>
    /// <param name="property">The same-named read-model property.</param>
    /// <returns>Whether exactly one comparison was proved.</returns>
    internal bool Matches(LambdaExpressionSyntax lambda, IPropertySymbol property)
    {
        _item = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => model.GetDeclaredSymbol(simple.Parameter),
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [var item] } => model.GetDeclaredSymbol(item),
            _ => null
        };
        var comparisons = lambda.Body is ExpressionSyntax body ? Conjuncts(body).Where(expression => IsFilter(expression, property)).ToList() : [];

        return comparisons.Count == 1;
    }

    /// <summary>
    /// Rejects additional uses, including repeated comparisons or computed values.
    /// </summary>
    /// <param name="declaration">The query declaration.</param>
    /// <returns>Whether every reference belongs to the proved filter.</returns>
    internal bool HasOnlyProvenUses(MethodDeclarationSyntax declaration) => declaration.DescendantNodes().OfType<IdentifierNameSyntax>()
        .Count(IsParameter) == (Optional ? 2 : 1);

    static bool IsNull(ExpressionSyntax expression) => QueryPredicates.Unwrap(expression).IsKind(SyntaxKind.NullLiteralExpression);

    static bool IsNullable(ITypeSymbol type) => type.NullableAnnotation == NullableAnnotation.Annotated ||
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };

    static IEnumerable<ExpressionSyntax> Conjuncts(ExpressionSyntax expression) => QueryPredicates.Unwrap(expression) switch
    {
        BinaryExpressionSyntax conjunction when conjunction.IsKind(SyntaxKind.LogicalAndExpression) => Conjuncts(conjunction.Left).Concat(Conjuncts(conjunction.Right)),
        var single => [single]
    };

    bool IsFilter(ExpressionSyntax expression, IPropertySymbol property)
    {
        expression = QueryPredicates.Unwrap(expression);
        if (IsEquality(expression, property))
        {
            return true;
        }

        if (expression is BinaryExpressionSyntax optional && optional.IsKind(SyntaxKind.LogicalOrExpression) && IsNullable(parameter.Type))
        {
            if (IsOptionalEquality(optional.Left, optional.Right, property) || IsOptionalEquality(optional.Right, optional.Left, property))
            {
                Optional = true;

                return true;
            }
        }

        return false;
    }

    bool IsOptionalEquality(ExpressionSyntax skip, ExpressionSyntax equality, IPropertySymbol property) =>
        IsNullSkip(QueryPredicates.Unwrap(skip)) && IsEquality(QueryPredicates.Unwrap(equality), property);

    bool IsNullSkip(ExpressionSyntax expression) => expression switch
    {
        IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax pattern } test => IsParameter(test.Expression) && IsNull(pattern.Expression),
        BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression) =>
            (IsParameter(binary.Left) && IsNull(binary.Right)) || (IsNull(binary.Left) && IsParameter(binary.Right)),
        _ => false
    };

    bool IsEquality(ExpressionSyntax expression, IPropertySymbol property) =>
        expression is BinaryExpressionSyntax equality && equality.IsKind(SyntaxKind.EqualsExpression) &&
        (AreOperands(equality.Left, equality.Right, property) || AreOperands(equality.Right, equality.Left, property));

    bool AreOperands(ExpressionSyntax member, ExpressionSyntax input, IPropertySymbol property) =>
        IsParameter(QueryPredicates.Unwrap(input)) && IsProperty(QueryPredicates.Unwrap(member), property);

    bool IsProperty(ExpressionSyntax expression, IPropertySymbol property)
    {
        if (expression is not MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver } member ||
            !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(member).Symbol, property))
        {
            return false;
        }

        return _item is not null && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, _item);
    }

    bool IsParameter(ExpressionSyntax expression) => expression is IdentifierNameSyntax &&
        SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(expression).Symbol, parameter);
}
