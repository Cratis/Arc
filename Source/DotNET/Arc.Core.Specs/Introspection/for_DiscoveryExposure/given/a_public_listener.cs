// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Authentication;
using Cratis.Arc.Http;
using Cratis.Arc.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.given;

[Collection("UsesCurrentDirectory")]
public class a_public_listener : Specification
{
    protected HttpListenerEndpointMapper _mapper;
    protected ServiceProvider _services;
    protected HttpStatusCode[] _statuses;
    string? _previousEnvironment;
    string _address;

    protected void Configure(string hostEnvironment, string processEnvironment, bool hasHandlers, bool? requireAuthentication = null)
    {
        _previousEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", processEnvironment);
        using var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        _address = $"http://127.0.0.1:{port}/";
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(hostEnvironment);
        var authentication = Substitute.For<IAuthentication>();
        authentication.HasHandlers.Returns(hasHandlers);
        authentication.HandleAuthentication(Arg.Any<IHttpRequestContext>()).Returns(AuthenticationResult.Anonymous);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(environment);
        services.AddSingleton(authentication);
        services.AddSingleton(Substitute.For<IHttpRequestContextAccessor>());
        services.Configure<ArcOptions>(options =>
        {
            if (requireAuthentication is bool required)
            {
                options.Introspection.RequireAuthentication = required;
            }
        });
        _services = services.BuildServiceProvider();
        _mapper = new HttpListenerEndpointMapper(NullLogger<HttpListenerEndpointMapper>.Instance, _address);
        var options = new IntrospectionOptions();
        if (requireAuthentication is bool required)
        {
            options.RequireAuthentication = required;
        }
        _mapper.MapIntrospectionEndpoints(options);
        _mapper.MapIdentityProviderEndpoint(_services);
    }

    protected async Task StartAndRequestDiscovery()
    {
        _mapper.Start(_services);
        using var client = new HttpClient { BaseAddress = new Uri(_address), Timeout = TimeSpan.FromSeconds(10) };
        var statuses = new List<HttpStatusCode>();
        foreach (var path in new[] { "/.cratis/commands", "/.cratis/queries", "/.cratis/users", "/.cratis/tenants", "/.cratis/identity-details/schema" })
        {
            using var response = await client.GetAsync(path);
            statuses.Add(response.StatusCode);
        }
        _statuses = [.. statuses];
    }

    async Task Destroy()
    {
        await _mapper.Stop();
        _mapper.Dispose();
        await _services.DisposeAsync();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", _previousEnvironment);
    }
}
