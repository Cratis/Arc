// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents the comparison a condition makes.
/// </summary>
public enum ComparisonKind
{
    /// <summary>
    /// The values are equal.
    /// </summary>
    Equal = 0,

    /// <summary>
    /// The values are not equal.
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// The left hand value is greater than the right hand value.
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// The left hand value is greater than or equal to the right hand value.
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// The left hand value is less than the right hand value.
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// The left hand value is less than or equal to the right hand value.
    /// </summary>
    LessThanOrEqual = 5
}
