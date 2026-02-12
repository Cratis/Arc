// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Http.for_WellKnownRoutesMiddleware.given;

public class a_well_known_routes_middleware : Specification
{
    protected WellKnownRoutesMiddleware _middleware;
    protected IServiceProvider _serviceProvider;
    protected ILogger<WellKnownRoutesMiddleware> _logger;
    protected HttpListener _listener;
    protected int _port;

    void Establish()
    {
        _port = Random.Shared.Next(50000, 60000);
        
        var services = new ServiceCollection();
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<IAuthentication, NoAuthentication>();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();

        _logger = Substitute.For<ILogger<WellKnownRoutesMiddleware>>();
        _middleware = new WellKnownRoutesMiddleware(_serviceProvider, _logger);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    void Destroy()
    {
        _listener?.Close();
    }

    class NoAuthentication : IAuthentication
    {
        public bool HasHandlers => false;

        public Task<AuthenticationResult> HandleAuthentication(IHttpRequestContext context) =>
            Task.FromResult(AuthenticationResult.Anonymous);
    }
}
