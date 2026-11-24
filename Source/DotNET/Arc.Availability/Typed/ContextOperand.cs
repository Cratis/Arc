// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The value of a provided read model that a <c>when</c> compares.
/// </summary>
/// <param name="readModel">The type of the provided read model.</param>
/// <param name="path">The path to the value, in camelCase.</param>
public sealed class ContextOperand(Type readModel, string path)
{
    /// <summary>
    /// The value is <see langword="true"/>.
    /// </summary>
    /// <returns>The condition.</returns>
    public ConditionTerm IsTrue() => Compare(ComparisonOperator.IsTrue);

    /// <summary>
    /// The value equals the operand.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm Is(object value) => Compare(ComparisonOperator.Equal, value);

    /// <summary>
    /// The value differs from the operand. An absent value is still unknown.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm IsNot(object value) => Compare(ComparisonOperator.NotEqual, value);

    /// <summary>
    /// The value equals any of the operands.
    /// </summary>
    /// <param name="values">The operands.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm In(params object[] values) => Compare(ComparisonOperator.In, values);

    /// <summary>
    /// The value equals none of the operands. An absent value is still unknown.
    /// </summary>
    /// <param name="values">The operands.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm NotIn(params object[] values) => Compare(ComparisonOperator.NotIn, values);

    /// <summary>
    /// The value is greater than the operand.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm IsGreaterThan(object value) => Compare(ComparisonOperator.GreaterThan, value);

    /// <summary>
    /// The value is greater than or equal to the operand.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm IsAtLeast(object value) => Compare(ComparisonOperator.GreaterThanOrEqual, value);

    /// <summary>
    /// The value is less than the operand.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm IsLessThan(object value) => Compare(ComparisonOperator.LessThan, value);

    /// <summary>
    /// The value is less than or equal to the operand.
    /// </summary>
    /// <param name="value">The operand.</param>
    /// <returns>The condition.</returns>
    public ConditionTerm IsAtMost(object value) => Compare(ComparisonOperator.LessThanOrEqual, value);

    ComparisonTerm Compare(ComparisonOperator op, params object[] operands) => new(readModel, path, op, operands);
}
