// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A next boundary before the evaluation instant ends the subject with an error instead of re-evaluating in a loop.
/// </summary>
public class when_the_boundary_is_before_the_evaluation : given.a_clock_boundary_subject
{
    void Establish()
    {
        _nextBoundary = reading => reading.Instant.AddHours(-1);
        Subscribe();
    }

    void Because() => _candidates.OnNext(1);

    [Fact] void should_forward_the_error() => _errors.Select(_ => _.GetType()).ShouldContainOnly(typeof(ClockBoundaryIsNotInTheFuture));
    [Fact] void should_evaluate_only_once() => _evaluations.ShouldEqual(1);
}
