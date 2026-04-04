// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class and_append_returns_failure : given.an_events_for_event_source_id_command_response_value_handler
{
    IEnumerable<EventForEventSourceId> _value;
    CommandResult _result;

    void Establish()
    {
        var violation = new ConstraintViolation(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            ConstraintType.Unknown,
            new ConstraintName("TestConstraint"),
            new ConstraintViolationMessage("Test violation"),
            new ConstraintViolationDetails());

        var failedResult = AppendResult.Failed(_correlationId, [violation]);
        _eventLog.Append(Arg.Any<EventSourceId>(), Arg.Any<object>()).Returns(failedResult);
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);

        _value = [new EventForEventSourceId(EventSourceId.New(), new TestEvent("Test"))];
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_failed_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_include_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_include_constraint_violation_message() => _result.ValidationResults.First().Message.ShouldEqual("Test violation");
}
