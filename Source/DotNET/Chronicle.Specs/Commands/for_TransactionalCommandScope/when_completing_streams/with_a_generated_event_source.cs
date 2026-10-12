// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_a_generated_event_source : given.a_pending_completion
{
    void Establish()
    {
        ResolveSource(new object());
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _handler = new(_eventLog);
        Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
        _eventLog.CompleteStream("completion", "period").Returns(completed);
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
    }

    [Fact] void should_have_a_generated_id_in_the_context() => ((EventSourceId)_context.Values[WellKnownCommandContextKeys.EventSourceId]).ShouldNotEqual(EventSourceId.Unspecified);
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_complete_across_all_sources() => _eventLog.Received(1).CompleteStream("completion", "period");
    [Fact] void should_not_scope_to_the_generated_id() => _eventLog.DidNotReceive().CompleteStream(Arg.Any<ClosedStreamScope>());
}
