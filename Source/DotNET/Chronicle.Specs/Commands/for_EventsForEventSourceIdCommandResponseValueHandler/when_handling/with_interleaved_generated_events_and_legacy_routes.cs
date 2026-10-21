// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_interleaved_generated_events_and_legacy_routes : given.a_mixed_generated_batch
{
    void Establish() => _events = [_events[0], _events[2], _events[1], _events[3]];
    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_all_four_events() => _stored.Length.ShouldEqual(4);
    [Fact] void should_preserve_the_returned_order() => _stored.Select(@event => ((Created)@event.Content).Name).ShouldEqual(["first", "third", "second", "fourth"]);
}
