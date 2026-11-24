// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The three-valued outcome of an availability evaluation. Only <see cref="Available"/> opens a gate.
/// </summary>
public enum AvailabilityOutcome
{
    /// <summary>
    /// The part is not available.
    /// </summary>
    Unavailable = 0,

    /// <summary>
    /// It cannot be told, because a value is absent or an audience is not resolved. Denies, like <see cref="Unavailable"/>.
    /// </summary>
    Unknown = 1,

    /// <summary>
    /// The part is available.
    /// </summary>
    Available = 2
}
