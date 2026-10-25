// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_async_scope_initialization_throws : given.a_post_request_with_execution_scopes
{
    bool _executed;

    void Establish() => _second.BeginAsync(Arg.Any<CommandContext>()).Returns(Task.FromException(new Exception("Initialization failed")));

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () =>
    {
        _executed = true;
        return Task.FromResult(_executedContext);
    });

    [Fact] void should_not_execute_the_action() => _executed.ShouldBeFalse();
    [Fact] void should_return_the_failure() => ResultingCommandResult.ExceptionMessages.ShouldContain("Initialization failed");
    [Fact] void should_complete_with_the_failure() => _completedResult.ShouldEqual(ResultingCommandResult);
    [Fact] void should_end_every_invocation_in_reverse() => _calls.TakeLast(2).ShouldEqual(["second.end:active", "first.end:active"]);
}
