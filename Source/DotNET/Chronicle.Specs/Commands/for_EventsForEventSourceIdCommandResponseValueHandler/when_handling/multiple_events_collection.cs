// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class multiple_events_collection : given.an_events_for_event_source_id_command_response_value_handler
{
    IEnumerable<EventForEventSourceId> _value;
    CommandResult _result;
    EventSourceId _firstEventSourceId;
    EventSourceId _secondEventSourceId;
    TestEvent _firstEvent;
    AnotherTestEvent _secondEvent;

    void Establish()
    {
        _firstEventSourceId = EventSourceId.New();
        _secondEventSourceId = EventSourceId.New();
        _firstEvent = new TestEvent("First");
        _secondEvent = new AnotherTestEvent(42);
        _value =
        [
            new EventForEventSourceId(_firstEventSourceId, _firstEvent),
            new EventForEventSourceId(_secondEventSourceId, _secondEvent)
        ];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_append_first_event_with_its_event_source_id() => (_appended[0].EventSourceId, _appended[0].Event).ShouldEqual((_firstEventSourceId, _firstEvent));
    [Fact] void should_append_second_event_with_its_event_source_id() => (_appended[1].EventSourceId, _appended[1].Event).ShouldEqual((_secondEventSourceId, _secondEvent));
    [Fact] void should_append_both_events() => _appended.Length.ShouldEqual(2);
    [Fact] void should_leave_each_guard_to_chronicle() => _scopes.Values.ShouldEachConformTo(_ => _ == ConcurrencyScope.NotSet);
    [Fact] void should_carry_a_scope_for_each_id() => _scopes.Keys.ShouldContainOnly(_firstEventSourceId, _secondEventSourceId);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
