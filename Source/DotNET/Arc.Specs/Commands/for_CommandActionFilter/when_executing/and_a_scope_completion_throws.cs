// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_a_scope_completion_throws : given.a_post_request_with_execution_scopes
{
    void Establish() => _second.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(Task.FromException(new Exception("Completion failed")));

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    [Fact] void should_return_internal_server_error() => _actionContext.HttpContext.Response.StatusCode.ShouldEqual(500);
    [Fact] void should_keep_the_error() => ResultingCommandResult.ExceptionMessages.ShouldContain("Completion failed");
    [Fact] void should_clear_the_response() => ResultingCommandResult.Response.ShouldBeNull();
    [Fact] void should_complete_the_remaining_scope() => _first.Received(1).Complete(Arg.Any<CommandContext>(), ResultingCommandResult);
    [Fact] void should_end_both_invocations() => _calls.ShouldContainOnly("first.begin", "second.begin", "first.beginAsync", "second.beginAsync", "first.complete:active", "second.end:active", "first.end:active");
}
