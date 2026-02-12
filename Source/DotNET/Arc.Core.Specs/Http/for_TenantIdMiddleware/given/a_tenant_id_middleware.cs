// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Tenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Http.for_TenantIdMiddleware.given;

public class a_tenant_id_middleware : Specification
{
    protected TenantIdMiddleware _middleware;
    protected IOptions<ArcOptions> _options;
    protected ILogger<TenantIdMiddleware> _logger;
    protected ArcOptions _arcOptions;
    protected HttpListener _listener;
    protected int _port;

    void Establish()
    {
        _port = Random.Shared.Next(50000, 60000);

        _arcOptions = new ArcOptions
        {
            Tenancy = new TenancyOptions
            {
                HttpHeader = "x-tenant-id"
            }
        };
        _options = Options.Create(_arcOptions);
        _logger = Substitute.For<ILogger<TenantIdMiddleware>>();
        _middleware = new TenantIdMiddleware(_options, _logger);

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{_port}/");
    }

    void Destroy()
    {
        _listener?.Close();
    }
}
