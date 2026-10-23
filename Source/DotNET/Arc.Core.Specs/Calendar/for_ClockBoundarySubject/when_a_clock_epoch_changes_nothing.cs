// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A clock epoch whose answer equals the last one is suppressed.
/// </summary>
public class when_a_clock_epoch_changes_nothing : given.a_clock_boundary_subject
{
    void Establish()
    {
        _nextBoundary = reading => reading.Instant < Start.AddHours(1) ? Start.AddHours(1) : null;
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because() => _time.Advance(TimeSpan.FromHours(1));

    [Fact] void should_evaluate_at_the_boundary() => _evaluations.ShouldEqual(2);
    [Fact] void should_not_emit_again() => Results.ShouldContainOnly(1);
}
