// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_action_captures_an_exception_with_details_hidden : given.a_post_request_with_execution_scopes
{
    string[] _scopeMessages;

    void Establish()
    {
        _options.ExposeExceptionDetails = false;
        _executedContext.Exception = new Exception("Action failed", new Exception("Inner failure"));
        _first.Complete(Arg.Any<CommandContext>(), Arg.Any<CommandResult>()).Returns(call =>
        {
            _scopeMessages = call.Arg<CommandResult>().ExceptionMessages.ToArray();
            return Task.CompletedTask;
        });
    }

    Task Because() => _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    [Fact] void should_hide_the_exception_chain() => ResultingCommandResult.ExceptionMessages.ShouldEqual([ExceptionDetailRedactor.RedactedMessage]);
    [Fact] void should_clear_the_stack_trace() => ResultingCommandResult.ExceptionStackTrace.ShouldEqual(string.Empty);
    [Fact] void should_complete_scopes_with_full_detail() => _scopeMessages.ShouldEqual(["Action failed", "Inner failure"]);
}
