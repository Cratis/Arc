// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_an_event_source_response : given.a_pending_completion
{
    void Establish()
    {
        _context = _context with { Response = (EventSourceId)"response-owner" };
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _handler = new(_eventLog);
        Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
        _eventLog.CompleteStream(new ClosedStreamScope(EventSourceId: "response-owner", EventStreamType: "completion", EventStreamId: "period")).Returns(completed);
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_scope_by_the_response_without_inventing_a_source_type() => _eventLog.Received(1).CompleteStream(new ClosedStreamScope(EventSourceId: "response-owner", EventStreamType: "completion", EventStreamId: "period"));
}
