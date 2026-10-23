// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A candidate is evaluated at the current instant and the answer emitted.
/// </summary>
public class when_a_candidate_arrives : given.a_clock_boundary_subject
{
    void Establish() => Subscribe();

    void Because() => _candidates.OnNext(5);

    [Fact] void should_evaluate_once() => _evaluations.ShouldEqual(1);
    [Fact] void should_evaluate_at_the_current_instant() => _readings[0].Instant.ShouldEqual(Start);
    [Fact] void should_emit_the_answer() => Results.ShouldContainOnly(5);
}
