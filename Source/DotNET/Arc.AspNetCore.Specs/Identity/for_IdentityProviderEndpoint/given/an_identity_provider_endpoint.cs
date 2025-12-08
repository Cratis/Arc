// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Identity.for_IdentityProviderEndpoint.given;

public class an_identity_provider_endpoint : Specification
{
    protected IIdentityProviderResultHandler _identityProviderResultHandler;
    protected IdentityProviderEndpoint _endpoint;
    protected HttpResponse _response;
    protected HttpContext _httpContext;

    void Establish()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();

        _identityProviderResultHandler = Substitute.For<IIdentityProviderResultHandler>();
        _endpoint = new(_identityProviderResultHandler);

        _response = _httpContext.Response;
    }
}
