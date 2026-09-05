// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Expressions;

/// <summary>
/// Converts a <see cref="ConditionModel"/> into the Screenplay condition it corresponds to.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <remarks>
/// Screenplay has no parentheses in conditions, so a nested tree flattens when printed. Only a left associative
/// tree survives a round trip with its meaning intact.
/// </remarks>
public class ConditionConverter(IScreenplayNaming naming)
{
    readonly MappingSourceConverter _sources = new(naming);

    /// <summary>
    /// Converts a condition.
    /// </summary>
    /// <param name="condition">The condition to convert.</param>
    /// <returns>The <see cref="ConditionSyntax"/>, or <see langword="null"/> when there is none.</returns>
    public ConditionSyntax? Convert(ConditionModel? condition) => condition switch
    {
        ComparisonCondition comparison => new ComparisonConditionSyntax(
            naming.ToPropertyPath(comparison.Left),
            ToOperator(comparison.Operator),
            _sources.Convert(comparison.Right),
            SourceLocation.Start),
        LogicalCondition logical when Convert(logical.Left) is { } left && Convert(logical.Right) is { } right =>
            new LogicalConditionSyntax(
                left,
                logical.IsOr ? LogicalOperator.Or : LogicalOperator.And,
                right,
                SourceLocation.Start),
        _ => null
    };

    /// <summary>
    /// Converts the comparison a condition makes.
    /// </summary>
    /// <param name="kind">The comparison to convert.</param>
    /// <returns>The <see cref="ComparisonOperator"/>.</returns>
    static ComparisonOperator ToOperator(ComparisonKind kind) => kind switch
    {
        ComparisonKind.NotEqual => ComparisonOperator.NotEqual,
        ComparisonKind.GreaterThan => ComparisonOperator.GreaterThan,
        ComparisonKind.GreaterThanOrEqual => ComparisonOperator.GreaterThanOrEqual,
        ComparisonKind.LessThan => ComparisonOperator.LessThan,
        ComparisonKind.LessThanOrEqual => ComparisonOperator.LessThanOrEqual,
        _ => ComparisonOperator.Equal
    };
}
