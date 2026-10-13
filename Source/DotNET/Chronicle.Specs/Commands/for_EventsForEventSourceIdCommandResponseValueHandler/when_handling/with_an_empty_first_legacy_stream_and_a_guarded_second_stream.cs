// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_an_empty_first_legacy_stream_and_a_guarded_second_stream : given.an_events_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId[] _value;
    EventSourceId _id;
    EventStreamId _emptyStream;
    EventStreamId _guardedStream;
    ConcurrencyScope _guard;

    void Establish()
    {
        _id = EventSourceId.New();
        _emptyStream = new("empty");
        _guardedStream = new("populated");
        _guard = new(EventSequenceNumber.First, _id, EventStreamType.All, _guardedStream, EventSourceType.Default);
        _value =
        [
            new(_id, new TestEvent("empty stream")) { EventStreamId = _emptyStream },
            new(_id, new TestEvent("guarded stream")) { EventStreamId = _guardedStream }
        ];
        _concurrencyScopeStrategy.GetScope(_id, EventStreamType.All, _emptyStream, EventSourceType.Default).Returns(ConcurrencyScope.None);
        _concurrencyScopeStrategy.GetScope(_id, EventStreamType.All, _guardedStream, EventSourceType.Default).Returns(_guard);
    }

    Task Because() => _handler.Handle(_commandContext, _value);

    [Fact] void should_resolve_the_first_stream_default() => _concurrencyScopeStrategy.Received(1).GetScope(_id, EventStreamType.All, _emptyStream, EventSourceType.Default);
    [Fact] void should_resolve_the_second_stream_default() => _concurrencyScopeStrategy.Received(1).GetScope(_id, EventStreamType.All, _guardedStream, EventSourceType.Default);
    [Fact] void should_preserve_the_second_stream_guard() => _batchScopes[_id].ShouldEqual(_guard);
    [Fact] void should_append_both_events() => _batch.Length.ShouldEqual(2);
}
