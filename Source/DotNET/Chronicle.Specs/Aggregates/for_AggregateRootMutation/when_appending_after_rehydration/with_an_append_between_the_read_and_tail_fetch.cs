// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Aggregates.for_AggregateRootMutation.when_appending_after_rehydration;

public class with_an_append_between_the_read_and_tail_fetch : given.an_aggregate_rehydrated_from_event_scenario
{
    AppendResult _result;

    protected override Task<IEventSequence> GetRehydrationEventSequence()
    {
        var sequence = Substitute.For<IEventSequence>();
        var readForwarded = false;
        var tailForwarded = false;
        var streamType = new TestAggregateRoot().GetEventStreamType();
        sequence.GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(async call =>
            {
                if (tailForwarded)
                {
                    await _scenario.EventSequence.Append(_eventSourceId, new Changed(), streamType);
                }
                var events = await _scenario.EventSequence.GetFromSequenceNumber(
                    call.ArgAt<EventSequenceNumber>(0),
                    call.ArgAt<EventSourceId>(1),
                    call.ArgAt<IEnumerable<EventType>>(2));
                readForwarded = true;
                return events;
            });
        sequence.GetTailSequenceNumber(Arg.Any<EventSourceId>(), Arg.Any<EventSourceType>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>())
            .Returns(async call =>
            {
                if (readForwarded)
                {
                    // On the old order this append lands after the read, immediately before the tail fetch.
                    await _scenario.EventSequence.Append(_eventSourceId, new Changed(), streamType);
                }
                var tail = await _scenario.EventSequence.GetTailSequenceNumber(
                    call.ArgAt<EventSourceId>(0),
                    call.ArgAt<EventSourceType>(1),
                    call.ArgAt<EventStreamType>(2),
                    call.ArgAt<EventStreamId>(3));
                tailForwarded = true;
                return tail;
            });
        return Task.FromResult(sequence);
    }

    async Task Because() => _result = await AppendFromLoadedAggregate();

    [Fact] void should_reject_the_append_after_the_captured_tail() => _result.HasConcurrencyViolations.ShouldBeTrue();
}
