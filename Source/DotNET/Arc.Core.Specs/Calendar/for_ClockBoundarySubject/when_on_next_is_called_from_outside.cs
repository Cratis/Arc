// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Calendar.for_ClockBoundarySubject;

/// <summary>
/// The values of the subject come from its candidates and its clock, so an outside OnNext is refused.
/// </summary>
public class when_on_next_is_called_from_outside : given.a_clock_boundary_subject
{
    Exception _exception;

    void Establish() => Subscribe();

    async Task Because() => _exception = await Catch.Exception(() =>
    {
        _subject.OnNext(1);
        return Task.CompletedTask;
    });

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<NotSupportedException>();
}
