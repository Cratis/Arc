// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_execution_scopes_are_registered : given.a_post_request_with_execution_scopes
{
    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, async () =>
    {
        await Task.Yield();
        _calls.Add($"action:{_ambient.Value}");
        return _executedContext;
    });

    [Fact] void should_run_the_scopes_in_order_and_unwind_in_reverse() => _calls.ShouldEqual(["first.begin", "second.begin", "first.beginAsync", "second.beginAsync", "action:active", "second.complete:active", "first.complete:active", "second.end:active", "first.end:active"]);
    [Fact] void should_complete_with_the_returned_result() => _completedResult.ShouldEqual(ResultingCommandResult);
    [Fact] void should_pass_the_request_services() => _scopeContext.ServiceProvider.ShouldEqual(_actionContext.HttpContext.RequestServices);
    [Fact] void should_pass_the_body_command() => _scopeContext.Command.ShouldEqual(_command);
    [Fact] void should_keep_the_response() => ResultingCommandResult.Response.ShouldEqual("response");
    [Fact] void should_return_success() => _actionContext.HttpContext.Response.StatusCode.ShouldEqual(200);
}
