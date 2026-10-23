// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_LocalDateChanges.when_finding_the_next_local_date_change;

/// <summary>
/// A wholly skipped date: December 29 is followed by December 31.
/// </summary>
public class in_apia_with_a_skipped_date : given.a_local_date_change
{
    DateTimeOffset _result;

    void Because() => _result = NextIn("Pacific/Apia", Utc("2011-12-29T12:00:00Z"));

    [Fact] void should_be_the_expected_instant() => _result.ShouldEqual(Utc("2011-12-30T10:00:00Z"));
}
