// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.ProxyGenerator.Scenarios.for_ObservableQueries.ControllerBased;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>
/// xUnit collection fixture that starts a single Kestrel web application for all scenario tests
/// in the collection, avoiding per-test-class server startup cost (~300-600ms each).
/// The fixture is created once before the first test in the collection and disposed after the last.
/// </summary>
public sealed class SharedScenarioWebHostFixture : IAsyncLifetime
{
    static volatile SharedScenarioWebHostFixture? _current;

    /// <summary>
    /// Gets the active fixture instance. Available after <see cref="InitializeAsync"/> and before <see cref="DisposeAsync"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed before the fixture is initialized or after it is disposed.</exception>
    public static SharedScenarioWebHostFixture Current =>
        _current ?? throw new InvalidOperationException(
            "SharedScenarioWebHostFixture is not initialized. Ensure the test class is in [Collection(ScenarioCollectionDefinition.Name)].");

    IHost? _host;

    /// <summary>
    /// Gets the running web host.
    /// </summary>
    public IHost Host => _host!;

    /// <summary>
    /// Gets the base URL the server is listening on.
    /// </summary>
    public string ServerUrl { get; private set; } = string.Empty;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        _current = this;

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseKestrel(options =>
            options.Listen(IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http1AndHttp2));
        builder.Logging.ClearProviders();
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(SharedScenarioWebHostFixture).Assembly);
        builder.Services.AddRouting();
        builder.AddCratisArc(_ => { });
        builder.Services.AddSingleton<ObservableControllerQueriesState>();

        var app = builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseWebSockets();
        app.UseRouting();
        app.UseCratisArc();
        app.MapControllers();

        _host = app;
        await app.StartAsync();

        var server = app.Services.GetRequiredService<IServer>();
        var addresses = server.Features.Get<IServerAddressesFeature>();
        ServerUrl = addresses?.Addresses.FirstOrDefault() ?? "http://localhost:5000";
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        _current = null;
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }
}
