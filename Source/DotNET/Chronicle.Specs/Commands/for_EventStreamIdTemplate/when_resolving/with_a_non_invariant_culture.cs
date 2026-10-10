// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands.for_EventStreamIdTemplate.when_resolving;

public class with_a_non_invariant_culture : Specification
{
    CultureInfo _original;
    string _result;

    void Establish()
    {
        _original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
    }

    void Because() => _result = EventStreamIdTemplate.Resolve("{Amount}|{Date}|{Timestamp}|{Offset}|{Status}", new
    {
        Amount = new Amount(1234.5m),
        Date = new DateOnly(2026, 10, 2),
        Timestamp = new DateTime(2026, 10, 2, 12, 30, 0, DateTimeKind.Utc),
        Offset = new DateTimeOffset(2026, 10, 2, 12, 30, 0, TimeSpan.FromHours(2)),
        Status = DayOfWeek.Friday
    }).Value;

    void Destroy() => CultureInfo.CurrentCulture = _original;

    [Fact] void should_format_every_part_independently_of_the_current_culture() => _result.ShouldEqual("1234.5|2026-10-02|2026-10-02T12:30:00.0000000Z|2026-10-02T12:30:00.0000000+02:00|Friday");

    record Amount(decimal Value) : ConceptAs<decimal>(Value);
}
