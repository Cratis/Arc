// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Represents the kind of value a query parameter takes.
/// </summary>
public enum QueryParameterType
{
    /// <summary>
    /// The parameter takes text.
    /// </summary>
    Text = 0,

    /// <summary>
    /// The parameter takes a number.
    /// </summary>
    Number = 1,

    /// <summary>
    /// The parameter takes a boolean.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// The parameter takes a date.
    /// </summary>
    Date = 3,

    /// <summary>
    /// The parameter takes a time.
    /// </summary>
    Time = 4,

    /// <summary>
    /// The parameter takes a unique identifier.
    /// </summary>
    UniqueId = 5
}
