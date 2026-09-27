// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.AspNetCore.Http;
using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.Http.for_AspNetCoreHttpRequestContext.given;

public class a_default_http_context : Specification
{
    protected DefaultHttpContext _httpContext;
    protected AspNetCoreHttpRequestContext _context;

    void Establish()
    {
        _httpContext = new DefaultHttpContext();
        _context = new AspNetCoreHttpRequestContext(_httpContext);
    }
}
