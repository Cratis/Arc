// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_a_fresh_generated_source_and_legacy_routes : given.a_mixed_generated_batch
{
    async Task Because() => await Execute();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_all_four_events() => _stored.Length.ShouldEqual(4);
    [Fact] void should_preserve_event_order() => _stored.Select(@event => ((Created)@event.Content).Name).ShouldEqual(["first", "second", "third", "fourth"]);
    [Fact] void should_preserve_each_route() => _stored.Select(@event => @event.Context.EventStreamType.Value).ShouldEqual(["created", "updated", "legacy-one", "legacy-two"]);
    [Fact] void should_append_both_generated_events() => _stored.Count(@event => @event.Context.EventSourceId == _id).ShouldEqual(2);
}
