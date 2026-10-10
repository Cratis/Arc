// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Chronicle.Testing.Commands;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Streams.for_StreamReads.when_reading;

public class with_events_arriving_after_the_tail : given.a_stream_decision
{
    StreamReads _reads;
    StreamDecision _decision;
    readonly List<string> _order = [];

    async Task Establish()
    {
        _scenario.Given.ForEventSource(_id).OnRoute(_route).Events(new PriorFact("prior"));
        var events = await _scenario.EventLog.GetForEventSourceIdAndEventTypes(_id, [], _route.EventStreamType, _route.EventStreamId, _route.EventSourceType);
        var prior = events.Single();
        var later = prior with { Context = prior.Context with { SequenceNumber = prior.Context.SequenceNumber.Value + 1 } };
        var log = Substitute.For<IEventLog>();
        log.GetTailSequenceNumber(_id, _route.EventSourceType, _route.EventStreamType, _route.EventStreamId).Returns(_ =>
        {
            _order.Add("tail");
            return Task.FromResult(prior.Context.SequenceNumber);
        });
        log.GetForEventSourceIdAndEventTypes(_id, Arg.Any<IEnumerable<EventType>>(), _route.EventStreamType, _route.EventStreamId, _route.EventSourceType).Returns(_ =>
        {
            _order.Add("events");
            return Task.FromResult<IImmutableList<AppendedEvent>>([prior, later]);
        });
        _reads = new(log, Substitute.For<ICommandContextAccessor>());
    }

    async Task Because() => _decision = await _reads.Stream(_id, _route);
    [Fact] void should_read_the_tail_before_the_facts() => _order.ShouldEqual(new[] { "tail", "events" });
    [Fact] void should_exclude_later_facts() => _decision.Events.Count.ShouldEqual(1);
    [Fact] void should_retain_the_guarded_tail() => _decision.ConcurrencyScope.SequenceNumber.ShouldEqual(_decision.Tail);
    [Fact] void should_stamp_every_returned_wrapper() => _decision.Append(new Approved(1, false)).Events.Single().EventStreamId.ShouldEqual(_route.EventStreamId);
    [Fact] void should_label_the_scope_by_the_source() => _decision.Append(new Approved(1, false)).ConcurrencyScopes.Single().Key.ShouldEqual(_id);
    [Fact] void should_find_the_captured_type() => _decision.Has<PriorFact>().ShouldBeTrue();
    [Fact] void should_select_the_captured_payload() => _decision.EventsOf<PriorFact>().Single().Value.ShouldEqual("prior");
    [Fact] void should_omit_scopes_for_an_identical_retry() => _decision.Append([]).ConcurrencyScopes.ShouldBeEmpty();
}
