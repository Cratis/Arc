// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// A failing evaluation fails the output and ends the operator.
/// </summary>
public class when_evaluate_throws : given.a_clock_boundary_subject
{
    void Establish()
    {
        _compute = (_, _) => throw new InvalidOperationException("evaluate failed");
        Subscribe();
    }

    void Because() => _candidates.OnNext(1);

    [Fact] void should_forward_the_error() => _errors.Select(_ => _.Message).ShouldContainOnly("evaluate failed");
    [Fact] void should_release_the_candidates() => _candidates.HasObservers.ShouldBeFalse();
}
