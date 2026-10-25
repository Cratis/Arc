// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands.for_CommandScenario;

namespace Cratis.Arc.Chronicle.Slices.for_Slice;

public class when_giving_events_without_a_source : given.a_slice
{
    async Task Because()
    {
        _slice.GivenEvents(new LedgerCredited(40m), new LedgerCredited(2m));
        _outcome = await _slice.Run();
    }

    [Fact] void should_execute_successfully() => _outcome.ShouldBeSuccessful();
    [Fact] async Task should_seed_the_commands_event_source() => await _outcome.ShouldHaveAppendedEvent<LedgerSettled>(_source, @event => @event.Balance == 42m);
}
