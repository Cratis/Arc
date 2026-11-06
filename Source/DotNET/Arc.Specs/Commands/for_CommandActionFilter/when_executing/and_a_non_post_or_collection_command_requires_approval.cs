// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity.Approvals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_a_non_post_or_collection_command_requires_approval : given.a_post_request_for_a_command_action
{
    [Theory]
    [InlineData("PUT", "single", false)]
    [InlineData("PUT", "single", true)]
    [InlineData("POST", "list", false)]
    [InlineData("POST", "list", true)]
    [InlineData("POST", "array", false)]
    [InlineData("POST", "array", true)]
    [InlineData("POST", "enumerable", false)]
    [InlineData("POST", "enumerable", true)]
    public async Task should_refuse_without_executing_the_action(string method, string shape, bool nullArgument)
    {
        await using var services = new ServiceCollection()
            .AddSingleton<IModelMetadataProvider, EmptyModelMetadataProvider>()
            .BuildServiceProvider();
        _actionContext.HttpContext.RequestServices = services;
        _actionContext.HttpContext.Request.Method = method;
        _actionContext.ActionDescriptor.Parameters[0].ParameterType = shape switch
        {
            "list" => typeof(List<ApprovalRequiringCommand>),
            "array" => typeof(ApprovalRequiringCommand[]),
            "enumerable" => typeof(IEnumerable<ApprovalRequiringCommand>),
            _ => typeof(ApprovalRequiringCommand)
        };
        _actionContext.ActionArguments["command"] = nullArgument ? null : shape switch
        {
            "list" => new List<ApprovalRequiringCommand>(),
            "array" or "enumerable" => Array.Empty<ApprovalRequiringCommand>(),
            _ => new ApprovalRequiringCommand()
        };
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
    }

    [RequiresApproval]
    public record ApprovalRequiringCommand;
}
