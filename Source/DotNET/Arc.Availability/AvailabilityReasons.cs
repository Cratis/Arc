// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The reasons an availability evaluation reports. They are distinct from authorization denial, so clients can tell
/// "not available for you" apart from "not allowed".
/// </summary>
public static class AvailabilityReasons
{
    /// <summary>
    /// The part is not available for the request.
    /// </summary>
    public const string NotAvailable = "NotAvailable";
}
