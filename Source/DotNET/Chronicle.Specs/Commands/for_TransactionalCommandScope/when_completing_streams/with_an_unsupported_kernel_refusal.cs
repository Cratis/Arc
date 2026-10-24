// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing_streams;

public class with_an_unsupported_kernel_refusal : given.a_pending_completion
{
    Exception? _error;

    void Establish()
    {
        ResolveSource(new given.SourceCompletion("owner"));
        _eventLog = Substitute.For<EventLogWithNamedTags>();
        _handler = new(_eventLog);
        _eventLog.CompleteStream(Arg.Any<ClosedStreamScope>()).Returns(Task.FromException<Result<EventSequenceNumber, CompleteStreamError>>(new ClosedStreamScopesNotSupported()));
    }

    async Task Because()
    {
        _result.MergeWith(await Enroll());
        _error = await Cratis.Specifications.Catch.Exception(() => _scope.Complete(_context, _result));
    }

    [Fact]
    async Task should_surface_an_unsupported_kernel_without_an_unscoped_retry()
    {
        _error.ShouldBeOfExactType<ClosedStreamScopesNotSupported>();
        await _eventLog.DidNotReceive().CompleteStream("completion", "period");
    }
}
