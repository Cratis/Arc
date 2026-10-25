// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_action_delegate_throws : given.a_post_request_with_execution_scopes
{
    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () => throw new Exception("Action failed"));

    [Fact] void should_return_the_failure() => ResultingCommandResult.ExceptionMessages.ShouldContain("Action failed");
    [Fact] void should_complete_with_the_failure() => _completedResult.ShouldEqual(ResultingCommandResult);
    [Fact] void should_clear_the_response() => ResultingCommandResult.Response.ShouldBeNull();
    [Fact] void should_end_every_invocation_in_reverse() => _calls.TakeLast(2).ShouldEqual(["second.end:active", "first.end:active"]);
}
