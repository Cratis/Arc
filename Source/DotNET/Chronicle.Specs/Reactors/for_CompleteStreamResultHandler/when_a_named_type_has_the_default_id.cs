// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors.SideEffects;

namespace Cratis.Arc.Chronicle.Reactors.for_CompleteStreamResultHandler;

public class when_a_named_type_has_the_default_id : given.a_stream_completion
{
    ReactorSideEffectFailure _failure;

    async Task Because()
    {
        _result = await _handler.Handle(_context, _store, new CompleteStream("orders", EventStreamId.Default));
        _result.TryGetError(out _failure);
    }

    [Fact] void should_refuse_as_a_side_effect_failure() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _failure.AppendFailures.Single().Errors.Single().ShouldEqual("The default stream cannot be completed.");
    [Fact] void should_not_complete_any_stream() => _eventLog.ReceivedCalls().ShouldBeEmpty();
}
