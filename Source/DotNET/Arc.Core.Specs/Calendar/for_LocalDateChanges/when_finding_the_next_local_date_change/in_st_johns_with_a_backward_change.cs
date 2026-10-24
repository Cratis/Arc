// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_LocalDateChanges.when_finding_the_next_local_date_change;

/// <summary>
/// The local date becomes Oct 25, returns to Oct 24 and becomes Oct 25 again within one night.
/// </summary>
public class in_st_johns_with_a_backward_change : given.a_local_date_change
{
    DateTimeOffset _first;
    DateTimeOffset _second;
    DateTimeOffset _third;
    DateTimeOffset _fourth;

    void Because()
    {
        _first = NextIn("America/St_Johns", Utc("1987-10-25T02:00:00Z"));
        _second = NextIn("America/St_Johns", _first);
        _third = NextIn("America/St_Johns", _second);
        _fourth = NextIn("America/St_Johns", _third);
    }

    [Fact] void should_change_forward_at_0230() => _first.ShouldEqual(Utc("1987-10-25T02:30:00Z"));
    [Fact] void should_change_back_at_0231() => _second.ShouldEqual(Utc("1987-10-25T02:31:00Z"));
    [Fact] void should_change_forward_again_at_0330() => _third.ShouldEqual(Utc("1987-10-25T03:30:00Z"));
    [Fact] void should_then_reach_the_next_midnight() => _fourth.ShouldEqual(Utc("1987-10-26T03:30:00Z"));
}
