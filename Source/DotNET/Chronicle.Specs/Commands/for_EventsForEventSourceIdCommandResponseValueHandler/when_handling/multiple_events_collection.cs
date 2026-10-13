// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

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
    [Fact] void should_append_first_event_with_its_event_source_id() => _batch[0].EventSourceId.ShouldEqual(_firstEventSourceId);
    [Fact] void should_keep_the_first_event() => _batch[0].Event.ShouldEqual(_firstEvent);
    [Fact] void should_append_second_event_with_its_event_source_id() => _batch[1].EventSourceId.ShouldEqual(_secondEventSourceId);
    [Fact] void should_keep_the_second_event() => _batch[1].Event.ShouldEqual(_secondEvent);
    [Fact] void should_append_as_one_batch() => _eventLog.Received(1).AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), concurrencyScopes: _batchScopes);
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
