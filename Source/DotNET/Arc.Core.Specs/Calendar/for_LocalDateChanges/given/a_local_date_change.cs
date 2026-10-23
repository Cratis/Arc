// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.Calendar.for_LocalDateChanges.given;

public class a_local_date_change : Specification
{
    protected static DateTimeOffset Utc(string instant) =>
        DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    protected static DateTimeOffset NextIn(string zoneId, DateTimeOffset from) =>
        LocalDateChanges.NextLocalDateChange(TimeZoneInfo.FindSystemTimeZoneById(zoneId), from);
}
