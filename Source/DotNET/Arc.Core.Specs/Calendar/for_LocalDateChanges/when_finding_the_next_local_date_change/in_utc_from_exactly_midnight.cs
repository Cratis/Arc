// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_LocalDateChanges.when_finding_the_next_local_date_change;

/// <summary>
/// The change is strictly after the instant, so from midnight it is the following midnight.
/// </summary>
public class in_utc_from_exactly_midnight : given.a_local_date_change
{
    DateTimeOffset _result;

    void Because() => _result = NextIn("UTC", Utc("2026-05-02T00:00:00Z"));

    [Fact] void should_be_the_expected_instant() => _result.ShouldEqual(Utc("2026-05-03T00:00:00Z"));
}
