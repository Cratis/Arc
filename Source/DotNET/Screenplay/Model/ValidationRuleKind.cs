// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the kind of a declarative validation rule.
/// </summary>
public enum ValidationRuleKind
{
    /// <summary>
    /// The value has to be present.
    /// </summary>
    NotEmpty = 0,

    /// <summary>
    /// The value has to be at most the operand.
    /// </summary>
    Max = 1,

    /// <summary>
    /// The value has to be at least the operand.
    /// </summary>
    Min = 2,

    /// <summary>
    /// The value has to be greater than the operand.
    /// </summary>
    GreaterThan = 3,

    /// <summary>
    /// The value has to be greater than or equal to the operand.
    /// </summary>
    GreaterThanOrEqual = 4,

    /// <summary>
    /// The value has to be less than the operand.
    /// </summary>
    LessThan = 5,

    /// <summary>
    /// The value has to be less than or equal to the operand.
    /// </summary>
    LessThanOrEqual = 6,

    /// <summary>
    /// The value has to equal the operand.
    /// </summary>
    Equal = 7,

    /// <summary>
    /// The length of the value has to equal the operand.
    /// </summary>
    Length = 8,

    /// <summary>
    /// The value has to match the pattern in the operand.
    /// </summary>
    Matches = 9,

    /// <summary>
    /// Every element of the collection has to be greater than the operand.
    /// </summary>
    AllGreaterThan = 10,

    /// <summary>
    /// Every element of the collection has to be greater than or equal to the operand.
    /// </summary>
    AllGreaterThanOrEqual = 11
}
