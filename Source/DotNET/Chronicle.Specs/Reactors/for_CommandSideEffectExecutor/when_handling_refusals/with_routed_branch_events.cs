// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_handling_refusals;

public class with_routed_branch_events : given.a_refused_invocation
{
    EventForEventSourceId _routed;
    RefusalRecorded _bare = new("bare");

    void Establish()
    {
        _routed = new("explicit", new RefusalRecorded("routed")) { EventStreamType = "explicit-type", EventStreamId = "explicit-stream", Subject = "explicit-subject", Tags = ["tag"] };
        _context.Values[WellKnownReactorContextKeys.EventSourceId] = (EventSourceId)"resolved";
        _context.Values[WellKnownReactorContextKeys.EventStreamType] = (EventStreamType)"resolved-type";
        _context.Values[WellKnownReactorContextKeys.EventStreamId] = (EventStreamId)"resolved-stream";
        _context.Values[WellKnownReactorContextKeys.EventSourceType] = (EventSourceType)"resolved-source";
        _context.Values[WellKnownReactorContextKeys.Subject] = (Subject)"resolved-subject";
        _invocation = Invoke.Command(_command).OnRefused(Refused.Any, _ => new object[] { _bare, _routed });
    }
    async Task Because() => _result = await _executor.Execute([_invocation], _context, _store);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_both_events_in_order() => _appended.Select(envelope => envelope.Event).ShouldContainOnly(_bare, _routed.Event);
    [Fact] void should_preserve_the_explicit_envelope() => ReferenceEquals(_appended[^1], _routed).ShouldBeTrue();
    [Fact] void should_use_the_resolved_source() => _appended[0].EventSourceId.Value.ShouldEqual("resolved");
    [Fact] void should_use_the_resolved_stream_type() => _appended[0].EventStreamType.Value.ShouldEqual("resolved-type");
    [Fact] void should_use_the_resolved_stream_id() => _appended[0].EventStreamId.Value.ShouldEqual("resolved-stream");
    [Fact] void should_use_the_resolved_source_type() => _appended[0].EventSourceType.Value.ShouldEqual("resolved-source");
    [Fact] void should_use_the_resolved_subject() => _appended[0].Subject!.Value.ShouldEqual("resolved-subject");
}
