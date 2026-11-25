// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// The form of an availability line: a line holds one.
/// </summary>
public enum AvailabilityForm
{
    /// <summary>
    /// A <see langword="for"/> over audiences.
    /// </summary>
    For = 0,

    /// <summary>
    /// A <see langword="with"/> over availabilities.
    /// </summary>
    With = 1,

    /// <summary>
    /// A <see langword="when"/> over a condition.
    /// </summary>
    When = 2
}
