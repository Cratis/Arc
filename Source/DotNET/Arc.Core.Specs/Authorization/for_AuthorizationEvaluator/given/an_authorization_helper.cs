// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Authorization.for_AuthorizationEvaluator.given;

public class an_authorization_helper : Specification
{
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IHttpRequestContext _httpRequestContext;
    protected AuthorizationEvaluator _authorizationHelper;
    protected ClaimsPrincipal _user;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _user = Substitute.For<ClaimsPrincipal>();

        _httpRequestContextAccessor.Current.Returns(_httpRequestContext);
        _httpRequestContext.User.Returns(_user);

        _authorizationHelper = new AuthorizationEvaluator(_httpRequestContextAccessor);
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

    protected void SetupNoHttpRequestContext()
    {
        _httpRequestContextAccessor.Current.Returns((IHttpRequestContext?)null);
    }

    protected void SetupNoUser()
    {
        _httpRequestContext.User.Returns((ClaimsPrincipal?)null);
    }
}