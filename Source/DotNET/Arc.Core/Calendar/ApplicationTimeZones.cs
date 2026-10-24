// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// Resolves time zone ids the application calendar accepts.
/// </summary>
static class ApplicationTimeZones
{
    const string FixedOffsetPrefix = "Etc/GMT";

    /// <summary>
    /// Resolves an IANA time zone id.
    /// </summary>
    /// <param name="id">The id to resolve.</param>
    /// <returns>The <see cref="TimeZoneInfo"/>.</returns>
    /// <exception cref="TimeZoneIsNotAnIanaZone">Thrown when the id is unknown, a Windows-only id or a non-zero fixed offset.</exception>
    internal static TimeZoneInfo Resolve(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new TimeZoneIsNotAnIanaZone(id ?? string.Empty, "the id is empty");
        }

        if (IsNonZeroFixedOffset(id))
        {
            throw new TimeZoneIsNotAnIanaZone(id, "it is a fixed-offset zone, not a place with a calendar");
        }

        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new TimeZoneIsNotAnIanaZone(id, $"the id is unknown on this host or its time-zone data is missing ({ex.GetType().Name})");
        }

        if (!zone.HasIanaId)
        {
            throw new TimeZoneIsNotAnIanaZone(id, "it is a Windows-only id, not an IANA id");
        }

        return zone;
    }

    /// <summary>
    /// Gets whether an id is <c>Etc/GMT+N</c> or <c>Etc/GMT-N</c> with a non-zero N.
    /// </summary>
    /// <param name="id">The id to check.</param>
    /// <returns>True for a non-zero fixed-offset id.</returns>
    static bool IsNonZeroFixedOffset(string id)
    {
        if (!id.StartsWith(FixedOffsetPrefix, StringComparison.Ordinal) || id.Length <= FixedOffsetPrefix.Length + 1)
        {
            return false;
        }

        var sign = id[FixedOffsetPrefix.Length];
        var digits = id[(FixedOffsetPrefix.Length + 1)..];
        return (sign is '+' or '-') && digits.All(char.IsAsciiDigit) && digits.Any(digit => digit != '0');
    }
}
