// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.given;

public class an_authorization_helper : Specification
{
    protected IHttpContextAccessor _httpContextAccessor;
    protected AuthorizationEvaluator _authorizationHelper;
    protected HttpContext _httpContext;
    protected ClaimsPrincipal _user;

    void Establish()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _authorizationHelper = new AuthorizationEvaluator(_httpContextAccessor);

        _httpContext = Substitute.For<HttpContext>();
        _user = Substitute.For<ClaimsPrincipal>();

        _httpContextAccessor.HttpContext.Returns(_httpContext);
        _httpContext.User.Returns(_user);
    }

    protected void SetupAuthenticatedUser(params string[] roles)
    {
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        _user.Identity.Returns(identity);

        foreach (var role in roles)
        {
            _user.IsInRole(role).Returns(true);
        }
    }

    protected void SetupUnauthenticatedUser()
    {
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(false);
        _user.Identity.Returns(identity);
    }

    protected void SetupNoHttpContext()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
    }

    protected void SetupNoUser()
    {
        _httpContext.User.Returns((ClaimsPrincipal?)null);
    }
}