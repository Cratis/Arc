// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_action_produced_a_response : given.a_post_request_for_a_command_action
{
    string _response;

    void Establish()
    {
        _response = "Forty two";
        _executedContext.Result = new ObjectResult(_response);
    }

    async Task Because() => await _filter.OnActionExecutionAsync(_actionContext, () => Task.FromResult(_executedContext));

    [Fact] void should_be_successful() => ResultingCommandResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_carry_the_response() => ResultingCommandResult.Response.ShouldEqual(_response);
}
