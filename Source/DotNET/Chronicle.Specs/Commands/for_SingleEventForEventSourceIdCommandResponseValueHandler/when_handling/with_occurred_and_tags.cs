// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_occurred_and_tags : given.a_single_event_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId _value;
    CommandResult _result;
    EventSourceId _eventSourceId;
    TestEvent _testEvent;
    DateTimeOffset _occurred;
    string[] _tags;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _testEvent = new TestEvent("Imported");
        _occurred = new DateTimeOffset(2019, 3, 14, 9, 26, 53, TimeSpan.Zero);
        _tags = ["imported", "batch-7"];
        _value = new EventForEventSourceId(_eventSourceId, _testEvent) { Occurred = _occurred, Tags = _tags };

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
            Arg.Any<Subject?>()).Returns(AppendResult.Success(_correlationId, EventSequenceNumber.First));
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _value);

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_with_the_supplied_tags_and_occurrence() => _eventLog.Received(1).Append(_eventSourceId, _testEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Is<IEnumerable<string>?>(_ => _ != null && _.SequenceEqual(_tags)), Arg.Any<ConcurrencyScope?>(), _occurred, Arg.Any<Subject?>());
}
