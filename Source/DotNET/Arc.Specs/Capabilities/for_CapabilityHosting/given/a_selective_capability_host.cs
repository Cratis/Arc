// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.Hosting;
using Cratis.Arc.Identity.Callers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Capabilities.for_CapabilityHosting.given;

public class a_selective_capability_host : Specification
{
    protected int _capabilityPort;
    protected int _defaultPort;
    protected int _selectedCalls;
    protected bool _outsideMintedCaller;
    private protected IVerifiedCallerAccessor _verified;
    protected WebApplication _app;

    protected virtual bool GateAfterRouting => false;
    protected virtual CapabilityEndpointSelector? Selector => new CapabilityEndpointSelector().ForRoutePattern("/mcp-spec");
    protected virtual bool ServerPathBase => false;
    protected virtual bool LatePathBase => false;
    protected virtual bool ExplicitRouting => false;
    protected virtual bool CaptureStartupError => false;
    protected Exception? _startupError;

    async Task Establish()
    {
        if (CaptureStartupError)
        {
            _startupError = await Catch.Exception(Start);
        }
        else
        {
            await Start();
        }
    }

    async Task Start() => _app = await KestrelPorts.Start(2, CreateHost, global::Xunit.TestContext.Current.CancellationToken);

    WebApplication CreateHost(int[] ports)
    {
        _capabilityPort = ports[0];
        _defaultPort = ports[1];
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{_capabilityPort}", $"http://127.0.0.1:{_defaultPort}");
        builder.AddCratisArc(options => options.EndpointGroups.Assign("capabilities", _capabilityPort), configureBuilder: arc => arc.WithoutControllers().WithCapabilities(options =>
        {
            options.GatedEndpoints = Selector;
            options.GateAfterRouting = GateAfterRouting;
        }));
        builder.Services.AddSingleton(new VerifiedCallerInstallation("https://resource.example", "test"));
        builder.Services.AddSingleton(new CallerResourceCatalog(new Dictionary<string, string> { ["https://resource.example"] = "audience" }));
        builder.Services.AddScoped<IVerifiedCredentialSource>(_ => Substitute.For<VerifiedCredentialSourceForSpecs>());
        _verified = Substitute.For<IVerifiedCallerAccessor>();
        _verified.GetCurrent().Returns(new CallerResolution(null, CallerRefusal.NoCredential));
        builder.Services.AddScoped(_ => _verified);
        if (ServerPathBase)
        {
            builder.Services.Insert(0, ServiceDescriptor.Singleton<IStartupFilter>(new PathBaseBeforeArc()));
        }
        var app = builder.Build();
        if (LatePathBase)
        {
            app.UsePathBase("/base");
        }
        ConfigurePipeline(app);
        if (ExplicitRouting)
        {
            app.UseRouting();
        }
        app.UseCratisArc();
        app.MapGet("/mcp-spec", Selected).WithMetadata(new GatedMarker());
        app.MapGet("/alias/mcp", Selected).WithMetadata(new GatedMarker());
        app.MapGet("/legacy/ping", Outside);
        app.MapGet("/legacy/{id}", (HttpContext context, ICapabilityCallers callers, string id) => Outside(context, callers));
        ConfigureEndpoints(app);
        return app;
    }

    protected virtual void ConfigurePipeline(WebApplication app)
    {
    }

    protected virtual void ConfigureEndpoints(WebApplication app)
    {
    }

    protected async Task<int> Request(string path, string method = "GET", string headers = "", bool defaultPort = false)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, defaultPort ? _defaultPort : _capabilityPort);
        await using var stream = client.GetStream();
        await using var writer = new StreamWriter(stream, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true };
        await writer.WriteAsync($"{method} {path} HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n{headers}\r\n");
        using var reader = new StreamReader(stream);
        var status = await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(10));
        return int.Parse(status!.Split(' ')[1], System.Globalization.CultureInfo.InvariantCulture);
    }

    async Task Destroy()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }

    string Selected()
    {
        Interlocked.Increment(ref _selectedCalls);
        return "selected";
    }

    string Outside(HttpContext context, ICapabilityCallers callers)
    {
        _outsideMintedCaller |= callers.Current is not null || context.Items.ContainsKey(CapabilityCallers.ItemKey) || context.Items.ContainsKey(VerifiedCallerPrincipal.ItemKey);
        return "legacy";
    }

    protected sealed class GatedMarker;

    sealed class PathBaseBeforeArc : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UsePathBase("/base");
            next(app);
        };
    }
}
