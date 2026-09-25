// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_EventsForEventSourceIdCommandResponseValueHandler.when_handling;

public class wrappers_with_occurred_and_tags_in_a_transaction : given.an_events_for_event_source_id_command_response_value_handler
{
    object[] _value;
    CommandResult _result;
    EventSourceId _commandEventSourceId;
    EventSourceId _wrapperEventSourceId;
    TestEvent _plainEvent;
    AnotherTestEvent _wrappedEvent;
    DateTimeOffset _occurred;
    string[] _tags;
    IUnitOfWork _unitOfWork;

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
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLog.Id.Returns(EventSequenceId.Log);
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        _result = await _handler.Handle(_commandContext, _value);
    }

    void Destroy() => CommandTransaction.Current = null;

    [Fact] void should_return_success() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_enroll_the_wrapped_event_with_its_tags_and_occurrence() => _unitOfWork.Received(1).AddEvent(EventSequenceId.Log, _wrapperEventSourceId, _wrappedEvent, Arg.Any<Causation>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<ConcurrencyScope?>(), Arg.Is<IEnumerable<string>?>(_ => _ != null && _.SequenceEqual(_tags)), _occurred, Arg.Any<Subject?>());
    [Fact] void should_enroll_the_plain_event_without_tags_or_occurrence() => _unitOfWork.Received(1).AddEvent(EventSequenceId.Log, _commandEventSourceId, _plainEvent, Arg.Any<Causation>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<ConcurrencyScope?>(), null, null, Arg.Any<Subject?>());
}
