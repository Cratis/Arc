// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_a_scope_completion_fails_the_result : given.a_post_request_with_execution_scopes
{
    void Establish() => _second.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(call =>
    {
        call.Arg<CommandResult>().ValidationResults = [ValidationResult.Error("Commit refused")];
        return Task.CompletedTask;
    });

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    [Fact] void should_not_be_successful() => ResultingCommandResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_clear_the_response() => ResultingCommandResult.Response.ShouldBeNull();
    [Fact] void should_return_bad_request() => _actionContext.HttpContext.Response.StatusCode.ShouldEqual(400);
    [Fact] void should_complete_the_remaining_scope() => _first.Received(1).Complete(Arg.Any<CommandContext>(), ResultingCommandResult);
    [Fact] void should_end_the_invocation() => ((ICommandInvocationExecutionScope)_first).Received(1).End(Arg.Any<CommandContext>());
}
