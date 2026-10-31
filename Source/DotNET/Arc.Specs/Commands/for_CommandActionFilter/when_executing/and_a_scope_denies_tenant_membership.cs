// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

public class and_a_scope_denies_tenant_membership : given.a_post_request_with_execution_scopes
{
    bool _executed;

    void Establish() => _first.When(scope => scope.Begin(Arg.Any<CommandContext>())).Do(_ => throw new TargetInvocationException(new TenantMembershipDenied()));

    Task Because() => _filter.OnActionExecutionAsync(_actionContext, () =>
    {
        _executed = true;
        return Task.FromResult(_executedContext);
    });

    [Fact] void should_respond_forbidden() => _actionContext.HttpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_not_execute_the_action() => _executed.ShouldBeFalse();
    [Fact] void should_be_unauthorized() => ResultingCommandResult.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_expose_exception_details() => ResultingCommandResult.HasExceptions.ShouldBeFalse();
    [Fact] void should_clear_the_response() => ResultingCommandResult.Response.ShouldBeNull();
}
