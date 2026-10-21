// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_the_same_id_across_legacy_streams_and_a_route_guard_violation : given.an_events_for_event_source_id_command_response_value_handler
{
    EventSourceId _id;
    CommandResult _result;

    void Establish()
    {
        _id = EventSourceId.New();
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>()).Returns(new AppendManyResult
        {
            CorrelationId = _correlationId,
            ConcurrencyViolations = [new ConcurrencyViolation(_id, EventSequenceNumber.First, (EventSequenceNumber)2ul)]
        });
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(_id, new TestEvent("first")) { EventStreamType = new EventStreamType("Transactions") },
        new(_id, new TestEvent("second")) { EventStreamType = new EventStreamType("Postings") }
    });

    [Fact] void should_fail_the_command() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_concurrency_violation() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
    [Fact] void should_attempt_exactly_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
}
