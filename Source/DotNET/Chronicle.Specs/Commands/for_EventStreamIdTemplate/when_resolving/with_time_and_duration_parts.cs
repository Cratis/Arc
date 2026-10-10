// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_time_and_duration_parts : Specification
{
    CultureInfo _original;
    string _result;

    void Establish()
    {
        _original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    }

    void Because() => _result = EventStreamIdTemplate.Resolve("{Time}|{Later}|{Duration}", new
    {
        Time = new TimeOnly(12, 30, 15).Add(TimeSpan.FromTicks(1234567)),
        Later = new TimeOnly(12, 30, 16).Add(TimeSpan.FromTicks(1234567)),
        Duration = new TimeSpan(1, 2, 3, 4).Add(TimeSpan.FromTicks(1234567))
    }).Value;

    void Destroy() => CultureInfo.CurrentCulture = _original;

    [Fact] void should_preserve_seconds_and_fractional_ticks_in_every_part() => _result.ShouldEqual("12:30:15.1234567|12:30:16.1234567|1.02:03:04.1234567");
}
