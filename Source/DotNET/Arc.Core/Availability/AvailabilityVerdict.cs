// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The three values of an availability decision.
/// </summary>
public enum AvailabilityVerdict
{
    /// <summary>
    /// The part is not available.
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// The decision could not be made from the known state. Unknown never opens a gate.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The part is available.
    /// </summary>
    Available = 2
}
