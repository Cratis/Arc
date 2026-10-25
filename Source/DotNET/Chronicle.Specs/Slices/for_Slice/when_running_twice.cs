// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;
using Cratis.Arc.Chronicle.Testing.Slices;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_running_twice : given.a_slice
{
    Exception _error;

    async Task Establish()
    {
        _slice.GivenEvents(new LedgerCredited(42m));
        _outcome = await _slice.Run();
    }

    async Task Because() => _error = await Catch.Exception(_slice.Run);

    [Fact] void should_refuse_a_second_run() => _error.ShouldBeOfExactType<SliceAlreadyRun>();
    [Fact] async Task should_not_append_the_event_twice() => await _slice.Then.ShouldHaveAppendedEventsInOrder(new LedgerSettled(42m));
}
