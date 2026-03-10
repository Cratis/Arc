// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class with_post_and_no_body_argument : given.a_command_action_filter
{
    CommandContext _capturedContext;
    ActionExecutingContext _actionContext;

    void Establish()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethod.Post.Method;

        var actionDescriptor = new ControllerActionDescriptor
        {
            ControllerName = "TestController",
            ActionName = "Execute",
            Parameters = []
        };

        var actionContext = new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            actionDescriptor,
            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());

        _actionContext = new ActionExecutingContext(
            actionContext,
            [],
            new Dictionary<string, object?>(),
            null!);

        _contextModifier
            .When(m => m.SetCurrent(Arg.Any<CommandContext>()))
            .Do(call => _capturedContext = call.Arg<CommandContext>());
    }

    async Task Because() => await _filter.OnActionExecutionAsync(
        _actionContext,
        () => Task.FromResult(new ActionExecutedContext(
            new ActionContext(
                _actionContext.HttpContext,
                _actionContext.RouteData,
                _actionContext.ActionDescriptor,
                _actionContext.ModelState),
            [],
            null!)));

    [Fact] void should_establish_a_command_context() => _contextModifier.Received(1).SetCurrent(Arg.Any<CommandContext>());
    [Fact] void should_use_object_as_command_type() => _capturedContext.Type.ShouldEqual(typeof(object));
}
