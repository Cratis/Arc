// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Calendar;

namespace Cratis.Arc.OptionsBinding;

/// <summary>
/// The binding table of <see cref="CalendarOptions"/>.
/// </summary>
internal static class CalendarOptionsBinding
{
    /// <summary>
    /// Gets how <see cref="CalendarOptions"/> is bound.
    /// </summary>
    internal static readonly ObjectType Bound = new(typeof(CalendarOptions), static () => new CalendarOptions(), Members);

    static Member[] Members() =>
    [
        Member.Of<CalendarOptions, string>("TimeZone", Leaves.String, static _ => _.TimeZone, static (o, v) => o.TimeZone = v)
    ];
}
