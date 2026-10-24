// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_LocalDateChanges.when_finding_the_next_local_date_change;

/// <summary>
/// A gap that skips midnight changes the date at the first valid instant.
/// </summary>
public class in_santiago_with_a_gap_over_midnight : given.a_local_date_change
{
    DateTimeOffset _result;

    void Because() => _result = NextIn("America/Santiago", Utc("2022-09-10T12:00:00Z"));

    [Fact] void should_be_the_expected_instant() => _result.ShouldEqual(Utc("2022-09-11T04:00:00Z"));
}
