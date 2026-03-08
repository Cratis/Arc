// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Arc.Http.for_AspNetCoreEndpointMapper.given;

public class an_endpoint_mapper : Specification
{
    protected WebApplication _app;
    protected AspNetCoreEndpointMapper _mapper;
    protected IEndpointRouteBuilder _routeBuilder;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        _app = builder.Build();
        _routeBuilder = _app;
        _mapper = new AspNetCoreEndpointMapper(_routeBuilder);
    }

    protected IReadOnlyList<Endpoint> GetEndpoints()
    {
        return _routeBuilder.DataSources
            .SelectMany(ds => ds.Endpoints)
            .ToList();
    }

    protected RouteEndpoint FindEndpoint(string pattern)
    {
        return GetEndpoints()
            .OfType<RouteEndpoint>()
            .First(e => e.RoutePattern.RawText == pattern);
    }

    void Destroy()
    {
        _app?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
