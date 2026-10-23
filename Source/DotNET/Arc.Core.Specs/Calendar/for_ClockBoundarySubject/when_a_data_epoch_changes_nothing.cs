// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A data epoch whose answer equals the last one is suppressed.
/// </summary>
public class when_a_data_epoch_changes_nothing : given.a_clock_boundary_subject
{
    void Establish()
    {
        Subscribe();
        _candidates.OnNext(1);
    }

    void Because() => _candidates.OnNext(1);

    [Fact] void should_evaluate_both() => _evaluations.ShouldEqual(2);
    [Fact] void should_emit_once() => Results.ShouldContainOnly(1);
}
