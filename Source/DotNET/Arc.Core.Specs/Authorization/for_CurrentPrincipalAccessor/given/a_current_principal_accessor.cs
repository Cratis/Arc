// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;

namespace Cratis.Arc.Authorization.for_CurrentPrincipalAccessor.given;

public class a_current_principal_accessor : Specification
{
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IHttpRequestContext _httpRequestContext;
    protected CurrentPrincipalAccessor _accessor;
    protected ClaimsPrincipal _requestUser;
    protected ClaimsPrincipal _systemUser;

    void Establish()
    {
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _requestUser = new ClaimsPrincipal(new ClaimsIdentity("http"));
        _systemUser = new ClaimsPrincipal(new ClaimsIdentity("system"));
        _httpRequestContext.User.Returns(_requestUser);
        _accessor = new CurrentPrincipalAccessor(_httpRequestContextAccessor);
    }

    protected void SetupHttpRequest() => _httpRequestContextAccessor.Current.Returns(_httpRequestContext);

    protected void SetupNoHttpRequest() => _httpRequestContextAccessor.Current.Returns((IHttpRequestContext?)null);
}
