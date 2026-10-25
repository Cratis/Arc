// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_first_scope_begin_throws : given.a_post_request_with_execution_scopes
{
    bool _executed;

    void Establish() => _first.When(_ => _.Begin(Arg.Any<CommandContext>())).Do(_ => throw new Exception("Begin failed"));

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () =>
    {
        _executed = true;
        return Task.FromResult(_executedContext);
    });

    [Fact] void should_not_execute_the_action() => _executed.ShouldBeFalse();
    [Fact] void should_return_the_failure() => ResultingCommandResult.ExceptionMessages.ShouldContain("Begin failed");
    [Fact] void should_complete_the_scope_that_started() => _first.Received(1).Complete(Arg.Any<CommandContext>(), ResultingCommandResult);
    [Fact] void should_not_complete_the_scope_that_never_started() => _second.DidNotReceive().Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>());
    [Fact] void should_end_only_the_scope_that_started() => _calls.TakeLast(1).ShouldEqual(["first.end:active"]);
    [Fact] void should_not_initialize_any_async_scope() => _calls.ShouldNotContain("first.beginAsync");
}
