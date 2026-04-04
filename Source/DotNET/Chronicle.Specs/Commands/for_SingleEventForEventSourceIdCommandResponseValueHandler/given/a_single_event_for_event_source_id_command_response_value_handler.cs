// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.given;

public class a_single_event_for_event_source_id_command_response_value_handler : Specification
{
    protected SingleEventForEventSourceIdCommandResponseValueHandler _handler;
    protected IEventLog _eventLog;
    protected IEventTypes _eventTypes;
    protected CommandContext _commandContext;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _eventLog = Substitute.For<IEventLog>();
        _eventTypes = Substitute.For<IEventTypes>();
        _handler = new SingleEventForEventSourceIdCommandResponseValueHandler(_eventLog, _eventTypes);

        _correlationId = Guid.NewGuid();
        var command = new TestCommand();
        _commandContext = new CommandContext(_correlationId, typeof(TestCommand), command, [], new CommandContextValues(), null);

        var successfulResult = AppendResult.Success(_correlationId, EventSequenceNumber.First);
        _eventLog.Append(
            Arg.Any<EventSourceId>(),
            Arg.Any<object>(),
            Arg.Any<EventStreamType?>(),
            Arg.Any<EventStreamId?>(),
            Arg.Any<EventSourceType?>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<ConcurrencyScope>()).Returns(successfulResult);
    }

    protected class TestCommand;

    protected record TestEvent(string Name);
    protected record AnotherTestEvent(int Value);
    protected record UnknownEvent(string Data);
}
