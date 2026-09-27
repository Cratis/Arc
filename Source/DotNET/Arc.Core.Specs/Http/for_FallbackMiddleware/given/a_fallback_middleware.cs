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
        _logger = Substitute.For<ILogger<FallbackMiddleware>>();
        _middleware = new FallbackMiddleware(_logger);
    }

    protected void StartListener()
    {
        (_listener, _port) = HttpListenerPorts.StartOnFreePort(port =>
        {
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            return listener;
        });
    }

    void Destroy()
    {
        _listener?.Close();
    }
}
