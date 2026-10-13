// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Testing.Commands;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Queries.for_QueryScenario.when_observing;

public class without_starting_an_observation : Specification
{
    readonly CommandScenario<ChangeObservedAccount> _scenario = new();
    readonly EventSourceId _id = EventSourceId.New();
    EventSequenceNumber _tail = EventSequenceNumber.Unavailable;
    IEnumerable<AppendedEvent> _events = [];

    void Establish() => _scenario.Given.ForEventSource(_id).Events(new ObservedAccountChanged(10m));

    async Task Because()
    {
        (await _scenario.Execute(new ChangeObservedAccount(_id, 42m))).IsSuccess.ShouldBeTrue();
        _tail = await _scenario.EventLog.GetTailSequenceNumber();
        _events = await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
    }

    [Fact] void should_keep_the_original_command_sequence_number() => _tail.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_leave_state_seeds_out_of_the_command_log() => _events.Select(@event => @event.Content).ShouldContainOnly(new ObservedAccountChanged(42m));
    [Fact] void should_capture_only_the_command_append() => _scenario.AppendedEvents.Count.ShouldEqual(1);

    void Destroy() => _scenario.Dispose();
}
