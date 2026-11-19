// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Converts a property value the Neo4j driver returned into what a <see cref="Cratis.Chronicle.Graphs.GraphRow"/> holds.
/// </summary>
/// <remarks>
/// Strings, booleans, whole numbers and floating-point numbers pass through. Temporal values become ISO 8601 strings, the
/// form Neo4j prints them in and the Chronicle hydrator reads them from: a zoned or offset date and time keeps its offset,
/// written <c>Z</c> when it is zero, and fractional seconds appear only when there are any. A duration becomes its ISO 8601
/// duration text. A list is converted item by item and a map entry by entry; null stays null.
/// </remarks>
public static class GraphDriverValues
{
    const string DateFormat = "yyyy-MM-dd";
    const string TimeFormat = "HH:mm:ss.FFFFFFF";
    const string DateTimeFormat = $"{DateFormat}'T'{TimeFormat}";

    /// <summary>
    /// Convert a value.
    /// </summary>
    /// <param name="value">The value the driver returned.</param>
    /// <returns>The converted value.</returns>
    public static object? Convert(object? value) => value switch
    {
        null => null,
        string => value,
        ZonedDateTime zoned => Iso(zoned.ToDateTimeOffset()),
        DateTimeOffset dateTimeOffset => Iso(dateTimeOffset),
        LocalDateTime local => local.ToDateTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture),
        LocalDate date => date.ToDateTime().ToString(DateFormat, CultureInfo.InvariantCulture),
        LocalTime time => Time(time.ToTimeSpan()),
        OffsetTime offsetTime => $"{Time(new TimeSpan(0, offsetTime.Hour, offsetTime.Minute, offsetTime.Second) + TimeSpan.FromTicks(offsetTime.Nanosecond / 100))}{Offset(TimeSpan.FromSeconds(offsetTime.OffsetSeconds))}",
        Duration duration => duration.ToString(),
        IDictionary dictionary => dictionary.Keys.Cast<object>().ToDictionary(_ => _.ToString()!, _ => Convert(dictionary[_]), StringComparer.Ordinal),
        IEnumerable list => list.Cast<object?>().Select(Convert).ToList(),
        _ => value
    };

    static string Iso(DateTimeOffset value) =>
        $"{value.DateTime.ToString(DateTimeFormat, CultureInfo.InvariantCulture)}{Offset(value.Offset)}";

    static string Time(TimeSpan time) => DateTime.MinValue.Add(time).ToString(TimeFormat, CultureInfo.InvariantCulture);

    static string Offset(TimeSpan offset) =>
        offset == TimeSpan.Zero ? "Z" : $"{(offset < TimeSpan.Zero ? '-' : '+')}{offset.Duration():hh\\:mm}";
}
