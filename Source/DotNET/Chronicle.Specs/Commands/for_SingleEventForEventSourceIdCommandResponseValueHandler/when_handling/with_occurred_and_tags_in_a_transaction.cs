// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_SingleEventForEventSourceIdCommandResponseValueHandler.when_handling;

public class with_occurred_and_tags_in_a_transaction : given.a_single_event_for_event_source_id_command_response_value_handler
{
    EventForEventSourceId _value;
    CommandResult _result;
    EventSourceId _eventSourceId;
    TestEvent _testEvent;
    DateTimeOffset _occurred;
    string[] _tags;
    IUnitOfWork _unitOfWork;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _testEvent = new TestEvent("Imported");
        _occurred = new DateTimeOffset(2019, 3, 14, 9, 26, 53, TimeSpan.Zero);
        _tags = ["imported"];
        _value = new EventForEventSourceId(_eventSourceId, _testEvent) { Occurred = _occurred, Tags = _tags };
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
    [Fact] void should_enroll_with_the_supplied_tags_and_occurrence() => _unitOfWork.Received(1).AddEvent(EventSequenceId.Log, _eventSourceId, _testEvent, Arg.Any<Causation>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<ConcurrencyScope?>(), Arg.Is<IEnumerable<string>?>(_ => _ != null && _.SequenceEqual(_tags)), _occurred, Arg.Any<Subject?>());
    [Fact] void should_not_append_immediately() => _eventLog.DidNotReceiveWithAnyArgs().Append(default!, default!);
}
