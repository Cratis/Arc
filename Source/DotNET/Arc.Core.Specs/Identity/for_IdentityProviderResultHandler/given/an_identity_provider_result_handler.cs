// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Identity.for_IdentityProviderResultHandler.given;

public class an_identity_provider_result_handler : Specification
{
    protected IHttpRequestContextAccessor _httpRequestContextAccessor;
    protected IHttpRequestContext _httpRequestContext;
    protected IProvideIdentityDetails _identityProvider;
    protected IServiceProvider _requestServices;
    protected IdentityProviderResultHandler _handler;
    protected ArcOptions _options = new();

    void Establish()
    {
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
        _httpRequestContextAccessor = Substitute.For<IHttpRequestContextAccessor>();
        _httpRequestContextAccessor.Current.Returns(_httpRequestContext);

        _identityProvider = Substitute.For<IProvideIdentityDetails>();
        _requestServices = Substitute.For<IServiceProvider>();
        _requestServices.GetService(typeof(IProvideIdentityDetails)).Returns(_identityProvider);
        _httpRequestContext.RequestServices.Returns(_requestServices);

        var optionsWrapper = Substitute.For<IOptions<ArcOptions>>();
        optionsWrapper.Value.Returns(_options);

        _handler = new(_httpRequestContextAccessor, optionsWrapper);
    }

    protected ClaimsPrincipal CreateAuthenticatedUser()
    {
        var identity = Substitute.For<ClaimsIdentity>();
        identity.IsAuthenticated.Returns(true);
        identity.Name.Returns("Test User");

        var claims = new List<Claim>
        {
            new("sub", "user123"),
            new(ClaimTypes.Name, "Test User")
        };

        var user = Substitute.For<ClaimsPrincipal>();
        user.Identity.Returns(identity);
        user.Claims.Returns(claims);

        return user;
    }
}
