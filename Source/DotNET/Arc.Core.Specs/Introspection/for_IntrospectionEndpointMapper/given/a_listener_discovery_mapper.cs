// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Cratis.Arc.Http.for_HttpListenerEndpointMapper.given;
using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Introspection.for_IntrospectionEndpointMapper.given;

public class a_listener_discovery_mapper : a_running_endpoint_mapper
{
    protected ArcOptions _options = new();
    protected string _environment = Environments.Development;
    protected string[] _routesBeforeStarting;
    protected string[] _routes;
    protected Action _beforeStarting = () => { };
    ServiceProvider _services;

    protected void MapDiscovery()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(_environment);
        var authentication = Substitute.For<IAuthentication>();
        authentication.HandleAuthentication(Arg.Any<IHttpRequestContext>()).Returns(AuthenticationResult.Anonymous);
        var services = new ServiceCollection();
        services.AddSingleton<IOptions<ArcOptions>>(Options.Create(_options));
        services.AddSingleton(environment);
        services.AddSingleton(authentication);
        services.AddSingleton<IHttpRequestContextAccessor, HttpRequestContextAccessor>();
        services.AddSingleton<IProvideIdentityDetails>(Substitute.For<IProvideIdentityDetails>());
        services.AddLogging();
        _services = services.BuildServiceProvider();
        _serviceProvider = _services;
        _endpointMapper.MapIntrospectionEndpoints(_options.Introspection);
        _endpointMapper.MapIdentityProviderEndpoint(_services);
        _routesBeforeStarting = _endpointMapper.Routes.Select(route => route.Pattern).ToArray();
        _beforeStarting();
        StartEndpointMapper();
        _routes = _endpointMapper.Routes.Select(route => route.Pattern).ToArray();
    }

    async Task Destroy()
    {
        await _endpointMapper.Stop();
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }
    }
}
