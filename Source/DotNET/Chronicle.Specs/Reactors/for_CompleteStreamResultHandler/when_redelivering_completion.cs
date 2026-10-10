// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Reactors.for_CompleteStreamResultHandler;

public class when_redelivering_completion : given.a_stream_completion
{
    void Establish()
    {
        Result<EventSequenceNumber, CompleteStreamError> completed = CompleteStreamError.AlreadyCompleted;
        _eventLog.CompleteStream("observed", "stream").Returns(completed);
    }

    async Task Because() => _result = await _handler.Handle(_context, _store, new CompleteStream());

    [Fact] void should_treat_already_completed_as_success() => _result.IsSuccess.ShouldBeTrue();
}
