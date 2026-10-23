// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_LocalDateChanges.when_finding_the_next_local_date_change;

/// <summary>
/// An ordinary local midnight in a zone with DST.
/// </summary>
public class in_oslo_at_an_ordinary_midnight : given.a_local_date_change
{
    DateTimeOffset _result;

    void Because() => _result = NextIn("Europe/Oslo", Utc("2026-03-28T12:00:00Z"));

    [Fact] void should_be_the_expected_instant() => _result.ShouldEqual(Utc("2026-03-28T23:00:00Z"));
}
