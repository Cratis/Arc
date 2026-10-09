// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;

namespace Cratis.Arc.Chronicle.Reactors.for_CompleteStreamResultHandler;

public class when_completing_the_observed_stream : given.a_stream_completion
{
    async Task Because() => _result = await _handler.Handle(_context, _store, new CompleteStream());

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_observed_event_route() => _eventLog.Received(1).CompleteStream("observed", "stream");
    [Fact] void should_recognize_the_return_type() => _handler.CanHandleReturnType(typeof(CompleteStream)).ShouldBeTrue();
}
