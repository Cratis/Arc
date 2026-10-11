// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class with_interleaved_effects : given.a_mixed_result_handler
{
    TestEvent _bare;
    EventForEventSourceId _routed;
    TestCommand _first;
    TestCommand _second;

    void Establish()
    {
        _bare = new("bare");
        _routed = new("explicit", new TestEvent("routed"))
        {
            EventStreamType = "explicit-type",
            EventStreamId = "explicit-stream",
            EventSourceType = "explicit-source",
            Subject = "explicit-subject",
            Tags = ["catalog"],
            NamedTags = [new NamedTag("tenant", "north")],
            Occurred = DateTimeOffset.UnixEpoch
        };
        _first = new("first");
        _second = new("second");
        _context.Values[WellKnownReactorContextKeys.EventSourceId] = (EventSourceId)"resolved";
        _context.Values[WellKnownReactorContextKeys.EventStreamType] = (EventStreamType)"resolved-type";
        _context.Values[WellKnownReactorContextKeys.EventStreamId] = (EventStreamId)"resolved-stream";
        _context.Values[WellKnownReactorContextKeys.EventSourceType] = (EventSourceType)"resolved-source";
        _context.Values[WellKnownReactorContextKeys.Subject] = (Subject)"resolved-subject";
    }

    async Task Because() => _result = await _dispatcher.Handle(_context, _store, new List<object> { _first, _bare, _second, _routed });

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_exactly_one_batch_before_commands() => _order.SequenceEqual(["append", "commands"]).ShouldBeTrue();
    [Fact] void should_preserve_event_order() => _appended.Select(envelope => envelope.Event).SequenceEqual([_bare, _routed.Event]).ShouldBeTrue();
    [Fact] void should_preserve_command_order() => _executed.SequenceEqual([_first, _second]).ShouldBeTrue();
    [Fact] void should_keep_the_explicit_envelope_with_all_its_metadata() => ReferenceEquals(_appended.Last(), _routed).ShouldBeTrue();
    [Fact] void should_use_the_resolved_source_for_bare_events() => _appended.First().EventSourceId.ShouldEqual((EventSourceId)"resolved");
    [Fact] void should_use_the_resolved_stream_type_for_bare_events() => _appended.First().EventStreamType.ShouldEqual((EventStreamType)"resolved-type");
    [Fact] void should_use_the_resolved_stream_id_for_bare_events() => _appended.First().EventStreamId.ShouldEqual((EventStreamId)"resolved-stream");
    [Fact] void should_use_the_resolved_source_type_for_bare_events() => _appended.First().EventSourceType.ShouldEqual((EventSourceType)"resolved-source");
    [Fact] void should_use_the_resolved_subject_for_bare_events() => _appended.First().Subject.ShouldEqual((Subject)"resolved-subject");
}
