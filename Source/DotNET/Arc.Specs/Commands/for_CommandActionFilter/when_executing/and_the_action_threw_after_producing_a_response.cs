// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

/// <summary>
/// A controller-based command bypasses the command pipeline entirely, so no execution scope ever runs for it. The
/// filter is the only place that can keep a value the action produced out of the error response body.
/// </summary>
public class and_the_action_threw_after_producing_a_response : given.a_post_request_for_a_command_action
{
    string _response;
    Exception _failure;

    void Establish()
    {
        _response = "Forty two";
        _failure = new Exception("Something went wrong");
        _executedContext.Result = new ObjectResult(_response);
        _executedContext.Exception = _failure;
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    [Fact] void should_not_be_successful() => ResultingCommandResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_carry_the_response() => ResultingCommandResult.Response.ShouldBeNull();
    [Fact] void should_keep_the_exception_message() => ResultingCommandResult.ExceptionMessages.ShouldContain(_failure.Message);
}
