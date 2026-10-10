// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Streams;
using Cratis.Chronicle.Events;

namespace Cratis.Arc.Chronicle.Commands.for_CompleteStreamCommandResponseValueHandler.when_handling;

public class and_a_named_type_has_the_default_id : given.a_sentinel_completion
{
    async Task Because() => _result = await _handler.Handle(_context, new CompleteStream("orders", EventStreamId.Default));

    [Fact] void should_refuse_as_validation() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_throw() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _result.ValidationResults.Single().Message.ShouldEqual("The default stream cannot be completed.");
    [Fact] void should_not_complete_any_stream() => _eventLog.ReceivedCalls().ShouldBeEmpty();
}
