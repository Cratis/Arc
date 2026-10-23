// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// An evaluation whose next boundary is not after its own instant is an evaluator error: the subject ends with an error
/// instead of re-evaluating in a loop.
/// </summary>
public class when_the_boundary_is_not_after_the_evaluation : given.a_clock_boundary_subject
{
    void Establish()
    {
        _nextBoundary = reading => reading.Instant;
        Subscribe();
    }

    void Because() => _candidates.OnNext(1);

    [Fact] void should_forward_the_error() => _errors.Select(_ => _.GetType()).ShouldContainOnly(typeof(ClockBoundaryIsNotInTheFuture));
    [Fact] void should_state_the_boundary() => ((ClockBoundaryIsNotInTheFuture)_errors[0]).Boundary.ShouldEqual(Start);
    [Fact] void should_state_the_evaluation_instant() => ((ClockBoundaryIsNotInTheFuture)_errors[0]).EvaluatedAt.ShouldEqual(Start);
    [Fact] void should_evaluate_only_once() => _evaluations.ShouldEqual(1);
    [Fact] void should_not_emit_the_result() => Results.ShouldBeEmpty();
    [Fact] void should_release_the_candidates() => _candidates.HasObservers.ShouldBeFalse();
}
