// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class with_subject : given.an_events_command_response_value_handler
{
    IEnumerable<object> _events;
    CommandResult _result;
    TestEvent _testEvent;
    AnotherTestEvent _anotherTestEvent;
    Subject _subject;

    void Establish()
    {
        _testEvent = new TestEvent("First Event");
        _anotherTestEvent = new AnotherTestEvent(123);
        _events = [_testEvent, _anotherTestEvent];

        _subject = new Subject("customer-42");
        _commandContext = new CommandContext(
            _correlationId,
            typeof(TestCommand),
            _commandContext.Command,
            [],
            new CommandContextValues
            {
                { WellKnownCommandContextKeys.EventSourceId, ((TestCommand)_commandContext.Command).EventSourceId },
                { WellKnownCommandContextKeys.Subject, _subject }
            },
            null);

        var successResult = AppendResult.Success(_correlationId, EventSequenceNumber.First);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope?>(),
            Arg.Any<DateTimeOffset?>(),
            Arg.Any<Subject?>()).Returns(successResult);
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_use_append_many() => _eventLog.DidNotReceive().AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>());
    [Fact] void should_append_each_event_individually() => _eventLog.Received(2).Append(Arg.Any<EventSourceId>(), Arg.Any<object>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Subject?>());
    [Fact] void should_append_first_event_with_subject() => _eventLog.Received(1).Append(Arg.Any<EventSourceId>(), _testEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), _subject);
    [Fact] void should_append_second_event_with_subject() => _eventLog.Received(1).Append(Arg.Any<EventSourceId>(), _anotherTestEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), _subject);
    [Fact] void should_return_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
