// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// No boundary arms no timer.
/// </summary>
public class when_no_boundary_is_returned : given.a_clock_boundary_subject
{
    void Establish() => Subscribe();

    void Because() => _candidates.OnNext(1);

    [Fact] void should_not_create_a_timer() => _time.TimersCreated.ShouldEqual(0);
}
