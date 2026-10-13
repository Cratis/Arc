// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_definition_routing_and_plain_or_untyped_events : given.a_definition_routed_batch
{
    async Task Establish()
    {
        _value = [new TestEvent("plain"), new EventForEventSourceId(_id, new TestEvent("wrapped"))];
        await CaptureSingleAppendRoutes();
    }

    Task Because() => _handler.Handle(_commandContext, _value);

    [Fact] void should_keep_each_event() => _batch.Select(_ => _.Event).ShouldEqual(_singleAppendRoutes.Select(_ => _.Event));
    [Fact] void should_match_single_append_definitions() => _batch.Select(_ => _.EventSource).ShouldEqual(_singleAppendRoutes.Select(_ => _.EventSource));
    [Fact] void should_match_single_append_streams() => _batch.Select(_ => _.EventStream).ShouldEqual(_singleAppendRoutes.Select(_ => _.EventStream));
    [Fact] void should_match_single_append_source_type_sentinels() => _batch.Select(_ => _.EventSourceType).ShouldEqual(_singleAppendRoutes.Select(_ => _.EventSourceType));
    [Fact] void should_match_single_append_stream_type_sentinels() => _batch.Select(_ => _.EventStreamType).ShouldEqual(_singleAppendRoutes.Select(_ => _.EventStreamType));
    [Fact] void should_match_single_append_stream_ids() => _batch.Select(_ => _.EventStreamId).ShouldEqual(_singleAppendRoutes.Select(_ => _.EventStreamId));
    [Fact] void should_match_single_append_subjects() => _batch.Select(_ => _.Subject).ShouldEqual(_singleAppendRoutes.Select(_ => _.Subject));
}
