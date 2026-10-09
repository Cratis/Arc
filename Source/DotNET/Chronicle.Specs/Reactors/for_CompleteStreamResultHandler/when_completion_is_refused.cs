// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reactors.SideEffects;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CompleteStreamResultHandler;

public class when_completion_is_refused : given.a_stream_completion
{
    ReactorSideEffectFailure _failure;

    void Establish()
    {
        Result<EventSequenceNumber, CompleteStreamError> refused = CompleteStreamError.DefaultStreamCannotBeCompleted;
        _eventLog.CompleteStream("observed", "stream").Returns(refused);
    }

    async Task Because()
    {
        _result = await _handler.Handle(_context, _store, new CompleteStream());
        _result.TryGetError(out _failure);
    }

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_name_the_stream() => _failure.AppendFailures.Single().Errors.Single().ShouldContain("observed/stream");
}
