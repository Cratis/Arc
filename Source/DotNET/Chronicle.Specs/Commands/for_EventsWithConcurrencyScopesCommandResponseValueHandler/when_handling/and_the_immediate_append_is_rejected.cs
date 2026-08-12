// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsWithConcurrencyScopesCommandResponseValueHandler.when_handling;

public class and_the_immediate_append_is_rejected : given.an_events_with_concurrency_scopes_command_response_value_handler
{
    CommandResult _result;
    EventsWithConcurrencyScopes _value;
    ConstraintViolation _constraintViolation;
    ConcurrencyViolation _concurrencyViolation;

    void Establish()
    {
        var target = EventSourceId.New();
        _value = new([new(target, new FirstEvent("first"))], []);
        _constraintViolation = new(
            EventTypeId.Unknown,
            EventSequenceNumber.Unavailable,
            ConstraintType.Unknown,
            new ConstraintName("UniqueValue"),
            new ConstraintViolationMessage("The value must be unique"),
            new ConstraintViolationDetails());
        _concurrencyViolation = new(target, new EventSequenceNumber(4), new EventSequenceNumber(5));
        _eventLog
            .AppendMany(
                Arg.Any<IEnumerable<EventForEventSourceId>>(),
                Arg.Any<CorrelationId?>(),
                Arg.Any<IEnumerable<string>?>(),
                Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>?>())
            .Returns(new AppendManyResult
            {
                CorrelationId = _correlationId,
                ConstraintViolations = [_constraintViolation],
                ConcurrencyViolations = [_concurrencyViolation]
            });
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_fail_as_an_ordinary_command_result() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_include_the_constraint_violation() => _result.ValidationResults.Any(_ => _.Message == _constraintViolation.Message.Value).ShouldBeTrue();
    [Fact] void should_include_the_concurrency_violation() => _result.ValidationResults.Any(_ => _.Reason == ValidationResultReason.ConcurrencyViolation).ShouldBeTrue();
}
