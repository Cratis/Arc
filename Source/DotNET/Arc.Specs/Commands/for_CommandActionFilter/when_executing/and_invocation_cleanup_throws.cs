// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_invocation_cleanup_throws : given.a_post_request_with_execution_scopes
{
    Exception _exception;

    void Establish() => ((ICommandInvocationExecutionScope)_second).When(_ => _.End(Arg.Any<CommandContext>())).Do(_ => throw new Exception("Cleanup failed"));

    async Task Because()
    {
        try
        {
            await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_propagate_the_cleanup_failure() => _exception.Message.ShouldEqual("Cleanup failed");
    [Fact] void should_end_the_remaining_invocation() => ((ICommandInvocationExecutionScope)_first).Received(1).End(Arg.Any<CommandContext>());
}
