// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries.for_QueryActionFilter.when_executing;

public class and_the_action_denies_tenant_membership : given.a_controller_query_request
{
    QueryResult _result;
    void Establish()
    {
        _options.ExposeExceptionDetails = true;
        _executed.Exception = new TargetInvocationException(new TenantMembershipDenied());
    }
    async Task Because()
    {
        await _filter.OnActionExecutionAsync(_actionContext, _next);
        _result = (QueryResult)((ObjectResult)(_actionContext.Result ?? _executed.Result)!).Value!;
    }
    [Fact] void should_respond_forbidden() => _httpContext.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_be_unauthorized() => _result.IsAuthorized.ShouldBeFalse();
    [Fact] void should_not_expose_details_even_when_enabled() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_handle_the_exception() => _executed.Exception.ShouldBeNull();
}
