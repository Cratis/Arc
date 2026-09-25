// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class wrappers_with_occurred_and_tags : given.an_events_for_event_source_id_command_response_value_handler
{
    object[] _value;
    CommandResult _result;
    EventSourceId _commandEventSourceId;
    EventSourceId _wrapperEventSourceId;
    TestEvent _plainEvent;
    AnotherTestEvent _wrappedEvent;
    DateTimeOffset _occurred;
    string[] _tags;

    void Establish()
    {
        _eventTypes.HasFor(Arg.Any<Type>()).Returns(true);
        _commandEventSourceId = EventSourceId.New();
        _wrapperEventSourceId = EventSourceId.New();
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceId] = _commandEventSourceId;
        _occurred = new DateTimeOffset(2019, 3, 14, 9, 26, 53, TimeSpan.Zero);
        _tags = ["imported"];

        _plainEvent = new TestEvent("Plain");
        _wrappedEvent = new AnotherTestEvent(42);
        _value = [_plainEvent, new EventForEventSourceId(_wrapperEventSourceId, _wrappedEvent) { Occurred = _occurred, Tags = _tags }];

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
    [Fact] void should_append_the_wrapped_event_with_its_tags_and_occurrence() => _eventLog.Received(1).Append(_wrapperEventSourceId, _wrappedEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), Arg.Is<IEnumerable<string>?>(_ => _ != null && _.SequenceEqual(_tags)), Arg.Any<ConcurrencyScope?>(), _occurred, Arg.Any<Subject?>());
    [Fact] void should_append_the_plain_event_without_tags_or_occurrence() => _eventLog.Received(1).Append(_commandEventSourceId, _plainEvent, Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<CorrelationId?>(), null, Arg.Any<ConcurrencyScope?>(), null, Arg.Any<Subject?>());
}
