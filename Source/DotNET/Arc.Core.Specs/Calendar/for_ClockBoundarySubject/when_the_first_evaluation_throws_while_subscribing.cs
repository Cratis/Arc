// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A source that replays its value while being subscribed to, with an evaluation that throws, fails the output and leaks
/// neither the subscription to the candidates nor a timer.
/// </summary>
public class when_the_first_evaluation_throws_while_subscribing : given.a_clock_boundary_subject
{
    BehaviorSubject<int> _replaying;

    void Establish()
    {
        _replaying = new BehaviorSubject<int>(1);
        _compute = (_, _) => throw new InvalidOperationException("evaluate failed");
        Build(_replaying, TimeZoneInfo.Utc);
    }

    void Because() => Subscribe();

    void Destroy() => _replaying.Dispose();

    [Fact] void should_forward_the_error() => _errors.Select(_ => _.Message).ShouldContainOnly("evaluate failed");
    [Fact] void should_release_the_candidates() => _replaying.HasObservers.ShouldBeFalse();
    [Fact] void should_arm_no_timer() => _time.TimersCreated.ShouldEqual(0);
}
