// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_model_state_is_invalid : given.a_post_request_with_execution_scopes
{
    bool _executed;

    void Establish() => _actionContext.ModelState.AddModelError("Value", "Invalid input");

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () =>
    {
        _executed = true;
        return Task.FromResult(_executedContext);
    });

    [Fact] void should_not_execute_the_action() => _executed.ShouldBeFalse();
    [Fact] void should_not_run_any_scopes() => _calls.ShouldBeEmpty();
    [Fact] void should_return_bad_request() => _actionContext.HttpContext.Response.StatusCode.ShouldEqual(400);
}
