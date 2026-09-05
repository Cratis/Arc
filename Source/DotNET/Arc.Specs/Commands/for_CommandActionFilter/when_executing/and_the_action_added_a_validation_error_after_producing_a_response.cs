// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_action_added_a_validation_error_after_producing_a_response : given.a_post_request_for_a_command_action
{
    const string ErrorMessage = "The value is not acceptable.";

    string _response;

    void Establish()
    {
        _response = "Forty two";
        _executedContext.Result = new ObjectResult(_response);
    }

    async Task Because() => await _filter.OnActionExecutionAsync(
        _actionContext,
        () =>
        {
            _actionContext.ModelState.AddModelError(nameof(TheCommand.Value), ErrorMessage);
            return Task.FromResult(_executedContext);
        });

    [Fact] void should_not_be_successful() => ResultingCommandResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_carry_the_response() => ResultingCommandResult.Response.ShouldBeNull();
    [Fact] void should_keep_the_validation_error() => ResultingCommandResult.ValidationResults.Select(_ => _.Message).ShouldContain(ErrorMessage);
}
