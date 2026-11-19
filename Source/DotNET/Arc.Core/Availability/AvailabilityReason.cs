// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Availability;

/// <summary>
/// The reasons Arc reports when a part of the application is refused because of availability.
/// </summary>
/// <remarks>
/// The value is carried in the authorization failure reason of a command or query result. It is distinct from an
/// authorization denial, so a client can tell "not available for you" apart from "not allowed", and it says no more than
/// that: an unavailable and an unknown decision are reported identically.
/// </remarks>
public static class AvailabilityReason
{
    /// <summary>
    /// The part is not available to the current context.
    /// </summary>
    public const string NotAvailable = "NotAvailable";
}
