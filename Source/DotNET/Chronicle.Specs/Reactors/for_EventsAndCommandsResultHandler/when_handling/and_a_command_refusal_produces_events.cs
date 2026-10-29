// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class and_a_command_refusal_produces_events : given.a_mixed_refusal
{
    void Establish()
    {
        _context.Values[WellKnownReactorContextKeys.EventSourceId] = (EventSourceId)"resolved";
        _context.Values[WellKnownReactorContextKeys.EventStreamType] = (EventStreamType)"resolved-type";
        _context.Values[WellKnownReactorContextKeys.EventStreamId] = (EventStreamId)"resolved-stream";
        _context.Values[WellKnownReactorContextKeys.EventSourceType] = (EventSourceType)"resolved-source";
        _context.Values[WellKnownReactorContextKeys.Subject] = (Subject)"resolved-subject";
    }
    async Task Because() => _result = await _handler.Handle(_context, _store, _events.Concat([_invocation, _later]).ToArray());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_two_distinct_events() => _trackedLog.Appended.Count.ShouldEqual(2);
    [Fact] void should_preserve_direct_keys() => _trackedLog.Attempts[0][0].IdempotencyKey.ShouldEqual(_directKeys[0]);
    [Fact] void should_keep_branch_keys_distinct() => _appended[0].IdempotencyKey.ShouldNotEqual(_trackedLog.Attempts[0][0].IdempotencyKey);
    [Fact] void should_use_the_resolved_source() => _appended[0].EventSourceId.Value.ShouldEqual("resolved");
    [Fact] void should_use_the_resolved_stream_type() => _appended[0].EventStreamType.Value.ShouldEqual("resolved-type");
    [Fact] void should_use_the_resolved_stream_id() => _appended[0].EventStreamId.Value.ShouldEqual("resolved-stream");
    [Fact] void should_use_the_resolved_source_type() => _appended[0].EventSourceType.Value.ShouldEqual("resolved-source");
    [Fact] void should_use_the_resolved_subject() => _appended[0].Subject!.Value.ShouldEqual("resolved-subject");
    [Fact] void should_skip_later_commands() => _commandPipeline.DidNotReceive().Execute(_later, _serviceProvider);
}
