// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// Finds the instants at which the local date of a time zone changes.
/// </summary>
public static class LocalDateChanges
{
    static readonly TimeSpan _sampleStep = TimeSpan.FromHours(1);
    static readonly TimeSpan _oneSecond = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Finds the earliest instant strictly after <paramref name="instant"/> at which the local date in <paramref name="zone"/>
    /// differs from the local date at <paramref name="instant"/>, forward or backward.
    /// </summary>
    /// <param name="zone">The time zone.</param>
    /// <param name="instant">The instant to search from.</param>
    /// <returns>The instant of the next local date change, in UTC.</returns>
    /// <remarks>
    /// It covers a local midnight, a gap that skips midnight, a wholly skipped date, and an offset transition that moves
    /// the local date back. Offset transitions that keep the date are stepped over.
    /// </remarks>
    public static DateTimeOffset NextLocalDateChange(TimeZoneInfo zone, DateTimeOffset instant)
    {
        var current = WholeSecond(instant.ToUniversalTime());
        var startDate = LocalDate(zone, current);

        while (true)
        {
            var offset = zone.GetUtcOffset(current);
            var localNow = current + offset;
            var midnight = new DateTimeOffset(localNow.Date.AddDays(1), TimeSpan.Zero) - offset;

            var transition = FindOffsetTransition(zone, current, midnight);
            if (transition is null)
            {
                return midnight;
            }

            if (LocalDate(zone, transition.Value) != startDate)
            {
                return transition.Value;
            }

            current = transition.Value;
        }
    }

    static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - (value.Ticks % TimeSpan.TicksPerSecond), TimeSpan.Zero);

    static DateOnly LocalDate(TimeZoneInfo zone, DateTimeOffset instant) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

    static DateTimeOffset? FindOffsetTransition(TimeZoneInfo zone, DateTimeOffset from, DateTimeOffset until)
    {
        var low = from;
        var lowOffset = zone.GetUtcOffset(low);
        while (low < until)
        {
            var high = low + _sampleStep < until ? low + _sampleStep : until;
            if (zone.GetUtcOffset(high) != lowOffset)
            {
                return Bisect(zone, low, high, lowOffset);
            }

            low = high;
        }

        return null;
    }

    static DateTimeOffset Bisect(TimeZoneInfo zone, DateTimeOffset low, DateTimeOffset high, TimeSpan lowOffset)
    {
        while (high - low > _oneSecond)
        {
            var seconds = (high - low).Ticks / TimeSpan.TicksPerSecond;
            var middle = low + TimeSpan.FromSeconds(seconds / 2);
            if (zone.GetUtcOffset(middle) == lowOffset)
            {
                low = middle;
            }
            else
            {
                high = middle;
            }
        }

        return high;
    }
}
