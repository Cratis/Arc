// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointsExtensions.given;

public class a_discovery_mapper : Specification
{
    protected ArcOptions _options = new();
    protected string _environment = Environments.Development;
    protected AspNetCoreEndpointMapper _mapper;
    WebApplication _app;

    protected void MapDiscovery()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = _environment });
        builder.Services.AddSingleton<IProvideIdentityDetails>(Substitute.For<IProvideIdentityDetails>());
        builder.Services.Configure<ArcOptions>(options => options.Introspection = _options.Introspection);
        _app = builder.Build();
        _mapper = new AspNetCoreEndpointMapper(_app);
        _mapper.MapIntrospectionEndpoints(_options.Introspection);
        _mapper.MapIdentityProviderEndpoint(_app.Services);
    }

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
