// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

#pragma warning disable CA1720 // Identifier contains type name. These are the Screenplay primitive names and cannot be renamed.

/// <summary>
/// Represents the primitive a concept is backed by.
/// </summary>
public enum ScreenplayPrimitive
{
    /// <summary>
    /// A universally unique identifier.
    /// </summary>
    Uuid = 0,

    /// <summary>
    /// Textual content.
    /// </summary>
    String = 1,

    /// <summary>
    /// A whole number.
    /// </summary>
    Int = 2,

    /// <summary>
    /// A fractional number.
    /// </summary>
    Decimal = 3,

    /// <summary>
    /// A boolean.
    /// </summary>
    Bool = 4,

    /// <summary>
    /// A date without a time component.
    /// </summary>
    Date = 5,

    /// <summary>
    /// A point in time.
    /// </summary>
    DateTime = 6,

    /// <summary>
    /// An enumeration, whose members are declared as values.
    /// </summary>
    Enum = 7
}

#pragma warning restore CA1720 // Identifier contains type name
