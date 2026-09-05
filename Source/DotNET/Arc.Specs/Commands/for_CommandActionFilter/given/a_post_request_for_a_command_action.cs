// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Commands.for_CommandActionFilter.given;

public class a_post_request_for_a_command_action : a_command_action_filter
{
    protected TheCommand _command;
    protected ActionExecutingContext _actionContext;
    protected ActionExecutedContext _executedContext;

    protected CommandResult<object> ResultingCommandResult => (CommandResult<object>)((ObjectResult)_executedContext.Result!).Value!;

    void Establish()
    {
        _command = new TheCommand("test-value");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethod.Post.Method;

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "TestController",
            ActionName = "Execute",
            Parameters =
            [
                new ControllerParameterDescriptor
                {
                    Name = "command",
                    ParameterType = typeof(TheCommand),
                    BindingInfo = new BindingInfo { BindingSource = BindingSource.Body }
                }
            ]
        };

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            actionDescriptor,
            new ModelStateDictionary());

        _actionContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?> { { "command", _command } },
            null!);

        _executedContext = new ActionExecutedContext(
            new ActionContext(
                _actionContext.HttpContext,
                _actionContext.RouteData,
                _actionContext.ActionDescriptor,
                _actionContext.ModelState),
            [],
            null!);
    }

    protected record TheCommand(string Value);
}
