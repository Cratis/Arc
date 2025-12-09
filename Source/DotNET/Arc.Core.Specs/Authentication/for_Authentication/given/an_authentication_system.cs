// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Authentication.for_Authentication.given;

public class an_authentication_system : Specification
{
    protected IInstancesOf<IAuthenticationHandler> _handlers;
    protected IHttpRequestContext _context;
    protected Authentication _authentication;

    void Establish()
    {
        _handlers = Substitute.For<IInstancesOf<IAuthenticationHandler>>();
        _context = Substitute.For<IHttpRequestContext>();
        _authentication = new Authentication(_handlers);
    }
}
