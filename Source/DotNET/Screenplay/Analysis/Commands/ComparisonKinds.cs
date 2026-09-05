// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Maps the comparison a C# operator makes onto the one a condition states, and relates one comparison to another.
/// </summary>
/// <remarks>
/// Reading a comparison and turning it around are two different jobs, and only the second one is arithmetic on the
/// operator itself. A branch is expressed by mirroring a comparison whose operands are the other way round, and the
/// branch not taken is expressed by negating it - neither of which needs to know anything about syntax.
/// </remarks>
public static class ComparisonKinds
{
    /// <summary>
    /// Converts a syntax kind into the comparison it makes.
    /// </summary>
    /// <param name="kind">The kind to convert.</param>
    /// <returns>The comparison, or <see langword="null"/> when the kind is not a comparison.</returns>
    public static ComparisonKind? Of(SyntaxKind kind) => kind switch
    {
        SyntaxKind.EqualsExpression => ComparisonKind.Equal,
        SyntaxKind.NotEqualsExpression => ComparisonKind.NotEqual,
        SyntaxKind.GreaterThanExpression => ComparisonKind.GreaterThan,
        SyntaxKind.GreaterThanOrEqualExpression => ComparisonKind.GreaterThanOrEqual,
        SyntaxKind.LessThanExpression => ComparisonKind.LessThan,
        SyntaxKind.LessThanOrEqualExpression => ComparisonKind.LessThanOrEqual,
        _ => null
    };

    /// <summary>
    /// Gets the comparison meaning the same thing with the operands the other way round.
    /// </summary>
    /// <param name="kind">The comparison to mirror.</param>
    /// <returns>The mirrored comparison.</returns>
    public static ComparisonKind Mirrored(ComparisonKind kind) => kind switch
    {
        ComparisonKind.GreaterThan => ComparisonKind.LessThan,
        ComparisonKind.GreaterThanOrEqual => ComparisonKind.LessThanOrEqual,
        ComparisonKind.LessThan => ComparisonKind.GreaterThan,
        ComparisonKind.LessThanOrEqual => ComparisonKind.GreaterThanOrEqual,
        _ => kind
    };

    /// <summary>
    /// Gets the comparison that is true exactly when another is false.
    /// </summary>
    /// <param name="kind">The comparison to negate.</param>
    /// <returns>The negated comparison.</returns>
    public static ComparisonKind Opposite(ComparisonKind kind) => kind switch
    {
        ComparisonKind.Equal => ComparisonKind.NotEqual,
        ComparisonKind.NotEqual => ComparisonKind.Equal,
        ComparisonKind.GreaterThan => ComparisonKind.LessThanOrEqual,
        ComparisonKind.GreaterThanOrEqual => ComparisonKind.LessThan,
        ComparisonKind.LessThan => ComparisonKind.GreaterThanOrEqual,
        _ => ComparisonKind.GreaterThan
    };
}
