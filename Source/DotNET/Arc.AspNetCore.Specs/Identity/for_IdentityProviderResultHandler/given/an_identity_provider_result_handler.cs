// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.given;

public class an_identity_provider_result_handler : Specification
{
    protected IHttpContextAccessor _httpContextAccessor;
    protected IProvideIdentityDetails _identityProvider;
    protected JsonSerializerOptions _serializerOptions;
    protected IdentityProviderResultHandler _handler;
    protected HttpContext _httpContext;
    protected ClaimsPrincipal _claimsPrincipal;

    void Establish()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Body = new MemoryStream();
        _httpContext.Response.Body = new MemoryStream();

        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext.Returns(_httpContext);

        _identityProvider = Substitute.For<IProvideIdentityDetails>();
        _serializerOptions = new JsonSerializerOptions();

        _claimsPrincipal = new ClaimsPrincipal();
        _httpContext.User = _claimsPrincipal;

        _handler = new(_httpContextAccessor, _identityProvider, _serializerOptions);
    }
}