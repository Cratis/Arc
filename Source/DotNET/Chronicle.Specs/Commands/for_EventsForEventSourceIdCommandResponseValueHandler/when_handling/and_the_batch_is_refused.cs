// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class and_the_batch_is_refused : given.an_events_for_event_source_id_command_response_value_handler
{
    CommandResult _result;

    void Establish() => _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>()).Returns(new AppendManyResult
    {
        CorrelationId = _correlationId,
        ConstraintViolations = [new(EventTypeId.Unknown, EventSequenceNumber.Unavailable, ConstraintType.Unknown, new ConstraintName("Refused"), new ConstraintViolationMessage("Batch refused"), new ConstraintViolationDetails())]
    });

    async Task Because() => _result = await _handler.Handle(_commandContext, new EventForEventSourceId[]
    {
        new(EventSourceId.New(), new TestEvent("first")),
        new(EventSourceId.New(), new TestEvent("second"))
    });

    [Fact] void should_return_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_refusal() => _result.ValidationResults.First().Message.ShouldEqual("Batch refused");
    [Fact] void should_attempt_exactly_one_batch() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.AppendMany)).ShouldEqual(1);
    [Fact] void should_not_append_individual_events() => _eventLog.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(_eventLog.Append)).ShouldEqual(0);
}
