// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_a_provided_event_source : given.a_pending_completion
{
    ClosedStreamScope _completion;

    void Establish()
    {
        ResolveSource(new ProvidedCompletion("provided-owner"));
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _handler = new(_eventLog);
        _completion = new(EventSourceId: "provided-owner", EventStreamType: "completion", EventStreamId: "period");
        Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
        _eventLog.CompleteStream(_completion).Returns(completed);
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_have_no_key_property() => _context.Command.HasEventSourceId().ShouldBeFalse();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_the_provided_source() => _eventLog.Received(1).CompleteStream(_completion);
    [Fact] void should_not_complete_across_all_sources() => _eventLog.DidNotReceive().CompleteStream("completion", "period");

    public record ProvidedCompletion(string Source) : ICanProvideEventSourceId
    {
        public EventSourceId GetEventSourceId() => Source;
    }
}
