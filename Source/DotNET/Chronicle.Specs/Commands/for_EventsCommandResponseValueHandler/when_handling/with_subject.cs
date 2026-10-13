// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Arc.Chronicle.Commands.for_EventsCommandResponseValueHandler.when_handling;

public class with_subject : given.an_events_command_response_value_handler
{
    object[] _events;
    EventForEventSourceId[] _batch;
    CommandResult _result;
    Subject _subject;

    void Establish()
    {
        _events = [new TestEvent("First Event"), new AnotherTestEvent(123)];
        _subject = new Subject("customer-42");
        _commandContext.Values[WellKnownCommandContextKeys.Subject] = _subject;
        _commandContext.Values[WellKnownCommandContextKeys.EventSourceType] = new EventSourceType("Customer");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamType] = new EventStreamType("Changes");
        _commandContext.Values[WellKnownCommandContextKeys.EventStreamId] = new EventStreamId("customer-changes");
        _eventLog.AppendMany(default!).ReturnsForAnyArgs(call =>
        {
            _batch = call.ArgAt<IEnumerable<EventForEventSourceId>>(0).ToArray();
            return AppendManyResult.Success(_correlationId, []);
        });
    }

    async Task Because() => _result = await _handler.Handle(_commandContext, _events);

    [Fact] void should_append_as_one_batch() => _eventLog.ReceivedWithAnyArgs(1).AppendMany(default!);
    [Fact] void should_preserve_the_events_in_order() => _batch.Select(_ => _.Event).ShouldEqual(_events);
    [Fact] void should_preserve_the_event_source_id() => _batch.ShouldEachConformTo(_ => _.EventSourceId == _commandContext.GetEventSourceId());
    [Fact] void should_preserve_the_subject() => _batch.ShouldEachConformTo(_ => _.Subject == _subject);
    [Fact] void should_preserve_the_source_type() => _batch.ShouldEachConformTo(_ => _.EventSourceType == new EventSourceType("Customer"));
    [Fact] void should_preserve_the_stream_type() => _batch.ShouldEachConformTo(_ => _.EventStreamType == new EventStreamType("Changes"));
    [Fact] void should_preserve_the_stream_id() => _batch.ShouldEachConformTo(_ => _.EventStreamId == new EventStreamId("customer-changes"));
    [Fact] void should_not_append_events_individually() => _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
}
