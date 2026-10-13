// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_compatible_default_legacy_guards : given.an_events_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId[] _value;
    EventSourceId _id;
    ConcurrencyScope _firstGuard;

    void Establish()
    {
        _id = EventSourceId.New();
        _value = [new(_id, new TestEvent("first")), new(_id, new TestEvent("second"))];
        _firstGuard = new(EventSequenceNumber.First, _id, EventStreamType.All, EventStreamId.Default, EventSourceType.Default);
        var laterGuard = new ConcurrencyScope(EventSequenceNumber.First + 1, _id, EventStreamType.All, EventStreamId.Default, EventSourceType.Default);
        _concurrencyScopeStrategy.GetScope(_id, EventStreamType.All, EventStreamId.Default, EventSourceType.Default).Returns(_firstGuard, laterGuard);
    }

    Task Because() => _handler.Handle(_commandContext, _value);

    [Fact] void should_resolve_the_default_guard_for_each_event() => _concurrencyScopeStrategy.Received(2).GetScope(_id, EventStreamType.All, EventStreamId.Default, EventSourceType.Default);
    [Fact] void should_keep_the_first_guard_for_the_shared_predicate() => _batchScopes[_id].ShouldEqual(_firstGuard);
    [Fact] void should_append_both_events() => _batch.Length.ShouldEqual(2);
}
