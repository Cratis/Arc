// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class with_post_and_body_argument : given.a_command_action_filter
{
    record TheCommand(string Value);

    CommandContext _capturedContext;
    TheCommand _command;
    ActionExecutingContext _actionContext;

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
    [Fact] void should_use_body_command_as_command_instance() => _capturedContext.Command.ShouldEqual(_command);
    [Fact] void should_use_body_command_type() => _capturedContext.Type.ShouldEqual(typeof(TheCommand));
    [Fact] void should_use_values_from_builder() => _capturedContext.Values.ShouldEqual(_builtValues);
    [Fact] void should_build_values_from_body_command() => _contextValuesBuilder.Received(1).Build(_command);
}
