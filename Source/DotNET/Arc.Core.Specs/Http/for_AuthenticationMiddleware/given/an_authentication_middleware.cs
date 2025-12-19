// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;

namespace Cratis.Arc.Http.for_AuthenticationMiddleware.given;

public class an_authentication_middleware : Specification
{
    protected IAuthentication _authentication;
    protected AuthenticationMiddleware _middleware;
    protected IHttpRequestContext _httpRequestContext;
    protected EndpointMetadata _metadata;

    void Establish()
    {
        _authentication = Substitute.For<IAuthentication>();
        _middleware = new AuthenticationMiddleware(_authentication);
        _httpRequestContext = Substitute.For<IHttpRequestContext>();
    }
}
