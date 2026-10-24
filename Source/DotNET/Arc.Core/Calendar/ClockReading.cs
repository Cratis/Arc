// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar;

/// <summary>
/// Represents one instant paired with the time zone whose calendar reads it.
/// </summary>
/// <param name="Instant">The instant.</param>
/// <param name="Zone">The time zone.</param>
public record ClockReading(DateTimeOffset Instant, TimeZoneInfo Zone)
{
    /// <summary>
    /// Gets the local date of <see cref="Instant"/> in <see cref="Zone"/>.
    /// </summary>
    public DateOnly Today => DateOf(Instant);

    /// <summary>
    /// Gets the local date of an instant in <see cref="Zone"/>.
    /// </summary>
    /// <param name="instant">The instant.</param>
    /// <returns>The local date.</returns>
    public DateOnly DateOf(DateTimeOffset instant) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, Zone).DateTime);
}
