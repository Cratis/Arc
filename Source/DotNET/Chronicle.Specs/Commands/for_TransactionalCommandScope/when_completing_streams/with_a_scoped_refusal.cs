// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_a_scoped_refusal : given.a_pending_completion
{
    [Theory]
    [InlineData(CompleteStreamError.EmptyScope)]
    [InlineData(CompleteStreamError.ExpectedTailMismatch)]
    public async Task should_surface_the_error_without_an_unscoped_retry(CompleteStreamError error)
    {
        ResolveSource(new given.SourceCompletion("owner"));
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _handler = new(_eventLog);
        Result<EventSequenceNumber, CompleteStreamError> refused = error;
        _eventLog.CompleteStream(Arg.Any<ClosedStreamScope>()).Returns(refused);
        _result.MergeWith(await Enroll());
        await _scope.Complete(_context, _result);
        _result.IsSuccess.ShouldBeFalse();
        _result.ValidationResults.Single().Message.ShouldContain(error.ToString());
        await _eventLog.DidNotReceive().CompleteStream("completion", "period");
    }
}
