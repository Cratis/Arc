// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_reading;

public class with_a_first_event_arriving_after_an_empty_tail : given.a_stream_decision
{
    StreamReads _reads;
    StreamDecision _decision;

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_id).OnRoute(_route).Events(new PriorFact("first event"));
        var arrived = await _scenario.EventLog.GetForEventSourceIdAndEventTypes(_id, [], _route.EventStreamType, _route.EventStreamId, _route.EventSourceType);
        var log = Substitute.For<IEventLog>();
        log.GetTailSequenceNumber(_id, _route.EventSourceType, _route.EventStreamType, _route.EventStreamId).Returns(EventSequenceNumber.Unavailable);
        log.GetForEventSourceIdAndEventTypes(_id, Arg.Any<IEnumerable<EventType>>(), _route.EventStreamType, _route.EventStreamId, _route.EventSourceType).Returns(arrived);
        _reads = new(log, Substitute.For<ICommandContextAccessor>());
    }

    async Task Because() => _decision = await _reads.Stream(_id, _route);

    [Fact] void should_keep_the_empty_snapshot() => _decision.Events.ShouldBeEmpty();
    [Fact] void should_report_the_stream_as_empty_at_the_tail() => _decision.IsEmpty.ShouldBeTrue();
    [Fact] void should_guard_the_first_append() => _decision.ConcurrencyScope.ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_not_decide_from_the_later_fact() => _decision.Has<PriorFact>().ShouldBeFalse();
}
