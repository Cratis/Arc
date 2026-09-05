// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

/// <summary>
/// Carries one property of each of the four .NET temporal types, so the generated proxy can be inspected for
/// whether the calendar-date/time-of-day types stay distinguishable from the two instant types on the wire.
/// </summary>
public class TypeWithTemporalProperties
{
    /// <summary>
    /// Gets or sets an instant without an offset.
    /// </summary>
    public DateTime DateTimeValue { get; set; }

    /// <summary>
    /// Gets or sets an instant with an offset.
    /// </summary>
    public DateTimeOffset DateTimeOffsetValue { get; set; }

    /// <summary>
    /// Gets or sets a calendar date - no time, no zone, no instant.
    /// </summary>
    public DateOnly DateOnlyValue { get; set; }

    /// <summary>
    /// Gets or sets a time of day - no date, no zone, no instant.
    /// </summary>
    public TimeOnly TimeOnlyValue { get; set; }
}
