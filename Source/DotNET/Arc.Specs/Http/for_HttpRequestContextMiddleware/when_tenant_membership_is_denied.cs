// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_HttpRequestContextMiddleware;

public class when_tenant_membership_is_denied : Specification
{
    HttpContext _context;
    Exception _exception;
    async Task Because()
    {
        _context = new DefaultHttpContext();
        var middleware = new HttpRequestContextMiddleware(Substitute.For<IHttpRequestContextAccessor>());
        _exception = await Catch.Exception(() => middleware.InvokeAsync(_context, _ => throw new TargetInvocationException(new TenantMembershipDenied())));
    }
    [Fact] void should_respond_forbidden() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
    [Fact] void should_handle_the_denial() => _exception.ShouldBeNull();
    [Fact] void should_not_write_exception_details() => _context.Response.ContentLength.ShouldBeNull();
}
