// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents the options for the application calendar.
/// </summary>
public class CalendarOptions
{
    /// <summary>
    /// Gets or sets the IANA id of the application time zone, such as <c>Europe/Oslo</c>. Defaults to <c>UTC</c>.
    /// </summary>
    /// <remarks>
    /// Windows-only ids and fixed-offset ids are refused when the host starts. Arc never replaces an invalid zone with UTC.
    /// </remarks>
    public string TimeZone { get; set; } = "UTC";
}
