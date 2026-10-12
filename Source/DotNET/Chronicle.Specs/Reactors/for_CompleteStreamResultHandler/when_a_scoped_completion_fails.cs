// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CompleteStreamResultHandler;

public class when_a_scoped_completion_fails : given.a_stream_completion
{
    [Theory]
    [InlineData(CompleteStreamError.EmptyScope)]
    [InlineData(CompleteStreamError.ExpectedTailMismatch)]
    public async Task should_surface_the_error_without_an_unscoped_retry(CompleteStreamError error)
    {
        Result<EventSequenceNumber, CompleteStreamError> refused = error;
        _eventLog.CompleteStream(Arg.Any<ClosedStreamScope>()).Returns(refused);
        var result = await _handler.Handle(_context, _store, new CompleteStream());
        result.IsSuccess.ShouldBeFalse();
        result.TryGetError(out var failure).ShouldBeTrue();
        failure.AppendFailures.Single().Errors.Single().ShouldContain(error.ToString());
        await _eventLog.DidNotReceive().CompleteStream("observed", "stream");
    }

    [Fact]
    public async Task should_propagate_an_unsupported_kernel_without_an_unscoped_retry()
    {
        _eventLog.CompleteStream(Arg.Any<ClosedStreamScope>()).Returns(Task.FromException<Result<EventSequenceNumber, CompleteStreamError>>(new ClosedStreamScopesNotSupported()));
        var error = await Cratis.Specifications.Catch.Exception(() => _handler.Handle(_context, _store, new CompleteStream()));
        error.ShouldBeOfExactType<ClosedStreamScopesNotSupported>();
        await _eventLog.DidNotReceive().CompleteStream("observed", "stream");
    }
}
