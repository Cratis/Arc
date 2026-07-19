// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalEventLog.when_appending;

public class and_events_for_event_source_ids_carry_metadata : Specification
{
    IEventLog _inner;
    IUnitOfWork _unitOfWork;
    TransactionalEventLog _eventLog;
    EventForEventSourceId _event;
    Causation _causation;
    Subject _subject;
    DateTimeOffset _occurred;

    void Establish()
    {
        _inner = Substitute.For<IEventLog>();
        _inner.Id.Returns(EventSequenceId.Log);
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _eventLog = new TransactionalEventLog(_inner);
        _causation = new Causation(DateTimeOffset.UtcNow, "Test", new Dictionary<string, string>());
        _subject = "the-subject";
        _occurred = DateTimeOffset.UtcNow.AddDays(-1);
        _event = new EventForEventSourceId(EventSourceId.New(), new object(), _causation)
        {
            EventStreamType = new EventStreamType("custom-stream"),
            EventStreamId = new EventStreamId("stream-42"),
            EventSourceType = new EventSourceType("customer"),
            Subject = _subject,
            Occurred = _occurred,
            Tags = ["tagged"]
        };
    }

    async Task Because()
    {
        CommandTransaction.Current = _unitOfWork;
        await _eventLog.AppendMany([_event]);
        CommandTransaction.Current = null;
    }

    [Fact] void should_enroll_the_event_with_its_metadata() => _unitOfWork.Received(1).AddEvent(
        EventSequenceId.Log,
        _event.EventSourceId,
        _event.Event,
        _causation,
        _event.EventStreamType,
        _event.EventStreamId,
        _event.EventSourceType,
        null,
        Arg.Is<IEnumerable<string>>(tags => tags.Contains("tagged")),
        _occurred,
        _subject);
}
