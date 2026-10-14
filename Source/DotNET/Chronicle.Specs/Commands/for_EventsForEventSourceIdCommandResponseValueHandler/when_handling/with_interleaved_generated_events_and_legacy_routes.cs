// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_interleaved_generated_events_and_legacy_routes : given.a_mixed_generated_batch
{
    void Establish() => _events = [_events[0], _events[2], _events[1], _events[3]];
    async Task Because()
    {
        _error = await Catch.Exception(() => _handler.Handle(_context, _events));
        _stored = (await _eventLog.GetFromSequenceNumber(EventSequenceNumber.First)).ToArray();
    }

    [Fact] void should_refuse_before_appending() => _error.ShouldBeOfExactType<GeneratedIdentifierEventsCannotBeSplit>();
    [Fact] void should_explain_the_unsplittable_guard() => _error.Message.ShouldContain("generated source");
    [Fact] void should_append_nothing() => _stored.ShouldBeEmpty();
}
