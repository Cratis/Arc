// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// The exception that is thrown when a time zone id cannot be used as an application or tenant time zone.
/// </summary>
/// <param name="timeZoneId">The id that was refused.</param>
/// <param name="cause">What is wrong with the id.</param>
/// <remarks>
/// Arc never falls back to UTC for a zone it cannot resolve, because that would move every local date without anyone noticing.
/// </remarks>
public class TimeZoneIsNotAnIanaZone(string timeZoneId, string cause)
    : Exception($"'{timeZoneId}' cannot be used as a time zone: {cause}. Use an IANA id such as 'Europe/Oslo' or 'UTC' that this host has time-zone data for. Arc never falls back to UTC.")
{
    /// <summary>
    /// Gets the id that was refused.
    /// </summary>
    public string TimeZoneId { get; } = timeZoneId;
}
