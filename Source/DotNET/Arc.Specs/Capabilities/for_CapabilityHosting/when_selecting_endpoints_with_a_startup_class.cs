// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting;

[Collection("UsesCurrentDirectory")]
public class when_selecting_endpoints_with_a_startup_class : Specification
{
    IHost _host;
    HttpClient _client;
    IVerifiedCallerAccessor _verified;
    given.startup_capability_endpoints _handlers;
    HttpStatusCode _legacy;
    HttpStatusCode _selected;
    int _outsideVerifications;

    async Task Establish()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var url = $"http://127.0.0.1:{port}";
        _verified = Substitute.For<IVerifiedCallerAccessor>();
        _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        _handlers = new();
        _host = Host.CreateDefaultBuilder()
            .AddCratisArc(options => options.EndpointGroups.Assign("capabilities", port))
            .ConfigureWebHostDefaults(web => web.UseUrls(url, "http://127.0.0.1:0").UseStartup<given.capability_startup>())
            .ConfigureServices(services =>
            {
                // Legacy IHostBuilder has no configureBuilder callback; use its actual service collection.
                var arc = Substitute.For<IArcBuilder>();
                arc.Services.Returns(services);
                arc.WithCapabilities(options => options.GatedEndpoints = new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec"));
                services.AddSingleton(_handlers);
                services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
                services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
                services.AddScoped<IVerifiedCredentialSource>(_ => Substitute.For<VerifiedCredentialSourceForSpecs>());
                services.AddScoped(_ => _verified);
            })
            .Build();
        await _host.StartAsync(global::Xunit.TestContext.Current.CancellationToken);
        _client = new HttpClient { BaseAddress = new Uri(url) };
    }

    async Task Because()
    {
        using var legacy = await _client.GetAsync("/legacy/ping", global::Xunit.TestContext.Current.CancellationToken);
        _legacy = legacy.StatusCode;
        _outsideVerifications = _verified.ReceivedCalls().Count();
        using var selected = await _client.GetAsync("/mcp-spec", global::Xunit.TestContext.Current.CancellationToken);
        _selected = selected.StatusCode;
    }

    async Task Destroy()
    {
        _client?.Dispose();
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    [Fact] void should_classify_the_startup_hosts_outside_endpoint() => _legacy.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_not_verify_the_outside_endpoint() => _outsideVerifications.ShouldEqual(0);
    [Fact] void should_gate_the_selected_endpoint() => _selected.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_not_execute_the_selected_handler() => _handlers.SelectedCalls.ShouldEqual(0);
}
