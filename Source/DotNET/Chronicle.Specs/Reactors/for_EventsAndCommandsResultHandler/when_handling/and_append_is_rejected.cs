// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class and_append_is_rejected : given.a_mixed_result_handler
{
    void Establish() => _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(AppendManyResult.Failed(CorrelationId.New(), [new AppendError("append refused")]));

    async Task Because() => _result = await _dispatcher.Handle(_context, _store, new object[] { new TestEvent("bare"), new TestCommand("next") });

    [Fact] void should_fail_the_reactor() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_surface_the_append_error() => (_result.TryGetError(out var failure) && failure.AppendFailures.SelectMany(item => item.Errors).Contains("append refused")).ShouldBeTrue();
    [Fact] void should_not_execute_commands() => _executor.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_attempt_only_one_append() => _eventLog.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "AppendMany").ShouldEqual(1);
}
