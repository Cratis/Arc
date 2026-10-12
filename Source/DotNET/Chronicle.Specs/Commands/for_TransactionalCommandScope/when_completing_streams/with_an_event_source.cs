// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_an_event_source : given.a_pending_completion
{
    ClosedStreamScope _completion;

    void Establish()
    {
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _handler = new(_eventLog);
        _context.Values[WellKnownCommandContextKeys.EventSourceId] = (EventSourceId)"owner";
        _context.Values[WellKnownCommandContextKeys.EventSourceType] = (EventSourceType)"reports";
        _completion = new("owner", "reports", "completion", "period");
        Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
        _eventLog.CompleteStream(_completion).Returns(completed);
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        _result.MergeWith(await _handler.Handle(_context, new Streams.CompleteStream("completion", "period")));
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_only_the_owning_source_once() => _eventLog.Received(1).CompleteStream(_completion);
    [Fact] void should_not_complete_across_all_sources() => _eventLog.DidNotReceive().CompleteStream("completion", "period");
}
