// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http.for_FallbackMiddleware.given;

public class a_fallback_middleware : Specification
{
    protected FallbackMiddleware _middleware;
    protected ILogger<FallbackMiddleware> _logger;
    protected HttpListener _listener;
    protected int _port;

    void Establish()
    {
        _port = Random.Shared.Next(50000, 60000);

        _logger = Substitute.For<ILogger<FallbackMiddleware>>();
        _middleware = new FallbackMiddleware(_logger);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    void Destroy()
    {
        _listener?.Close();
    }
}
