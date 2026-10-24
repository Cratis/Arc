// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// The first evaluation is always emitted, even when it is the default answer.
/// </summary>
public class when_the_first_evaluation_is_empty : given.a_clock_boundary_subject
{
    void Establish() => Subscribe();

    void Because() => _candidates.OnNext(0);

    [Fact] void should_emit_it() => Results.ShouldContainOnly(0);
}
