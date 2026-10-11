// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Reactors.for_EventsAndCommandsResultHandler.when_handling;

public class and_append_is_pending : given.a_mixed_result_handler
{
    TaskCompletionSource<AppendManyResult> _append;
    bool _commandsStartedBeforeAppendCompleted;

    void Establish()
    {
        _append = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventLog.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()).Returns(_append.Task);
    }

    async Task Because()
    {
        var handling = _handler.Handle(_context, _store, new object[] { new TestEvent("bare"), new TestCommand("next") });
        _commandsStartedBeforeAppendCompleted = _executed.Any();
        _append.SetResult(AppendManyResult.Success(CorrelationId.New(), []));
        _result = await handling.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_for_append_success_before_executing_commands() => _commandsStartedBeforeAppendCompleted.ShouldBeFalse();
    [Fact] void should_execute_commands_after_append_completes() => _executed.ShouldContainOnly(new TestCommand("next"));
}
