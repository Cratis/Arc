// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// Defines an attribute that gates a part of the application with one line.
/// </summary>
public interface IAvailabilityAttribute
{
    /// <summary>
    /// Gets the line the attribute states. Several attributes on one part are several lines, and all of them must hold.
    /// </summary>
    AvailabilityTerm Term { get; }
}
