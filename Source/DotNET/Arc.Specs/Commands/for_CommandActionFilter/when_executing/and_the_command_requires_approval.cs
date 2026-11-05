// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_the_command_requires_approval : given.a_post_request_for_a_command_action
{
    void Establish()
    {
        _actionContext.ActionArguments["command"] = new ApprovalRequiringCommand();
        _actionContext.ActionDescriptor.Parameters[0].ParameterType = typeof(ApprovalRequiringCommand);
        ((ControllerActionDescriptor)_actionContext.ActionDescriptor).MethodInfo = typeof(Controller).GetMethod(nameof(Controller.Handle))!;
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task should_refuse_without_executing_the_action_or_beginning_approval(bool registerFlow, bool validateOnly, bool nullArgument)
    {
        if (nullArgument) _actionContext.ActionArguments["command"] = null;
        var flow = Substitute.For<IApprovalFlow>();
        var registrations = new ServiceCollection();
        if (registerFlow) registrations.AddSingleton(flow);
        await using var services = registrations.BuildServiceProvider();
        _actionContext.HttpContext.RequestServices = services;
        if (validateOnly) _actionContext.HttpContext.Request.Path = "/commands/validate";
        var nextCalled = false;
        await _filter.OnActionExecutionAsync(_actionContext, () =>
        {
            nextCalled = true;
            return Task.FromResult(_executedContext);
        });

        nextCalled.ShouldBeFalse();
        _actionContext.HttpContext.Response.StatusCode.ShouldEqual(403);
        var result = (CommandResult<object>)((ObjectResult)_actionContext.Result!).Value!;
        result.IsAuthorized.ShouldBeFalse();
        result.AuthorizationFailureReason.ShouldEqual(ApprovalGate.RefusalReason);
        flow.ReceivedCalls().ShouldBeEmpty();
    }

    [RequiresApproval]
    public record ApprovalRequiringCommand;
    public class Controller { public IActionResult Handle(ApprovalRequiringCommand command) => new OkResult(); }
}
