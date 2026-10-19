// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

/// <summary>
/// Without a transaction, two events for one id that need different legacy-stream guards are appended as one batch that
/// carries both guards, so a violation of either refuses the whole batch.
/// </summary>
public class same_id_events_with_different_legacy_streams_and_an_implicit_guard_without_a_transaction : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _id;
    KeyValuePair<EventSourceId, ConcurrencyScope>[] _guards = [];

    void Establish()
    {
        _id = EventSourceId.New();
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _eventLog.Id.Returns(EventSequenceId.Log);
        _commandContext.Values[WellKnownCommandContextKeys.ConcurrencyDimensions] = ConcurrencyDimensions.EventStreamId;
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("command-stream");
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>()).Returns(call =>
        {
            _appended = call.Arg<IEnumerable<EventForEventSourceId>>().ToArray();
            _guards = call.Arg<IEnumerable<KeyValuePair<EventSourceId, ConcurrencyScope>>>().ToArray();
            return AppendManyResult.Success(_correlationId, []);
        });
    }

    async Task Because() => await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(_id, new TestEvent("first")) { EventStreamId = new EventStreamId("stream-a") },
        new(_id, new TestEvent("second")) { EventStreamId = new EventStreamId("stream-b") }
    });

    [Fact] void should_append_both_events() => _appended.Length.ShouldEqual(2);
    [Fact] void should_carry_one_guard_per_stream() => _guards.Select(_ => _.Value.EventStreamId).ShouldContainOnly(new EventStreamId("stream-a"), new EventStreamId("stream-b"));
    [Fact] void should_label_every_guard_with_the_event_source() => _guards.All(_ => _.Key == _id).ShouldBeTrue();
    [Fact] void should_append_exactly_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
}
