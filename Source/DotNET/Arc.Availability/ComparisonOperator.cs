// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The operators of a <see cref="ContextComparison"/>.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// The value is <see langword="true"/>.
    /// </summary>
    IsTrue = 0,

    /// <summary>
    /// The value equals the operand.
    /// </summary>
    Equal = 1,

    /// <summary>
    /// The value differs from the operand. An absent value is still unknown.
    /// </summary>
    NotEqual = 2,

    /// <summary>
    /// The value equals any of the operands.
    /// </summary>
    In = 3,

    /// <summary>
    /// The value equals none of the operands. An absent value is still unknown.
    /// </summary>
    NotIn = 4,

    /// <summary>
    /// The value is greater than the operand.
    /// </summary>
    GreaterThan = 5,

    /// <summary>
    /// The value is greater than or equal to the operand.
    /// </summary>
    GreaterThanOrEqual = 6,

    /// <summary>
    /// The value is less than the operand.
    /// </summary>
    LessThan = 7,

    /// <summary>
    /// The value is less than or equal to the operand.
    /// </summary>
    LessThanOrEqual = 8
}
