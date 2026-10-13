// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_incompatible_default_legacy_guards : given.an_events_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId[] _value;
    Exception _exception;

    void Establish()
    {
        var id = EventSourceId.New();
        _value =
        [
            new(id, new TestEvent("first stream")) { EventStreamId = new("first") },
            new(id, new TestEvent("second stream")) { EventStreamId = new("second") }
        ];
    }

    async Task Because() => _exception = await Catch.Exception(() => _handler.Handle(_commandContext, _value));

    [Fact] void should_reject_the_incompatible_guards() => _exception.ShouldBeOfExactType<IncompatibleConcurrencyScopesForEventSource>();
    [Fact] void should_not_append_any_batch() => _eventLog.DidNotReceiveWithAnyArgs().AppendMany(default!);
    [Fact] void should_not_append_individually() => _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!);
}
