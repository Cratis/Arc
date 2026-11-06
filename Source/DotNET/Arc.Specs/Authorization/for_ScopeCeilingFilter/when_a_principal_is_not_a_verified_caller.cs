// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Authorization.for_ScopeCeilingFilter;

public class when_a_principal_is_not_a_verified_caller : Specification
{
    AuthorizationFilterContext _context;

    void Establish()
    {
        var http = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scope", "other")], "product")) };
        var descriptor = new ControllerActionDescriptor
        {
            ControllerTypeInfo = typeof(ScopedController).GetTypeInfo(),
            MethodInfo = typeof(ScopedController).GetMethod(nameof(ScopedController.Read))!
        };
        _context = new AuthorizationFilterContext(new ActionContext(http, new RouteData(), descriptor), []);
    }

    void Because() => new ScopeCeilingFilter().OnAuthorization(_context);

    [Fact] void should_not_narrow_the_principal() => _context.Result.ShouldBeNull();

    [RequiresScope("content:write")]
    public class ScopedController : ControllerBase
    {
        public string Read() => "read";
    }
}
