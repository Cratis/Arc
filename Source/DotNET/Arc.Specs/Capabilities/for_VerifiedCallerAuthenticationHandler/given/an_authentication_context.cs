// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_VerifiedCallerAuthenticationHandler.given;

public class an_authentication_context : Specification
{
    protected DefaultHttpContext _context;
    ServiceProvider _services;

    void Establish()
    {
        var services = new ServiceCollection().AddLogging();
        VerifiedCallerAuthentication.Register(services);
        VerifiedCallerAuthentication.Register(services);
        _services = services.BuildServiceProvider();
        _context = new DefaultHttpContext { RequestServices = _services };
    }

    void Destroy() => _services.Dispose();
}
