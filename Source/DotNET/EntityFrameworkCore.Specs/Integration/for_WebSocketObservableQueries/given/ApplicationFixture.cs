// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Arc.EntityFrameworkCore.Observe;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.given;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - handled by IAsyncLifetime
#pragma warning disable CA2000 // Dispose objects before losing scope

/// <summary>
/// Shared fixture for all WebSocket observable query integration specs.
/// Sets up a single ArcApplication instance shared across all tests in the collection.
/// </summary>
public class ApplicationFixture : IAsyncLifetime
{
    public Uri BaseUri { get; private set; }
    public Uri WebSocketBaseUri { get; private set; }
    public IServiceProvider Services { get; private set; }
    public SqliteConnection Connection { get; private set; }
    public IEntityChangeTracker EntityChangeTracker { get; private set; }

    ArcApplication _app;

    static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async Task InitializeAsync()
    {
        var port = GetAvailablePort();
        BaseUri = new Uri($"http://localhost:{port}/");
        WebSocketBaseUri = new Uri($"ws://localhost:{port}/");

        Connection = new SqliteConnection("Data Source=:memory:");
        await Connection.OpenAsync();

        var builder = ArcApplication.CreateBuilder();

        builder.Services.AddSingleton(Connection);

        builder.Services.AddEntityFrameworkCoreObservation();

        builder.Services.AddPooledDbContextFactory<IntegrationTestDbContext>((sp, options) =>
        {
            var conn = sp.GetRequiredService<SqliteConnection>();
            options.UseSqlite(conn)
                   .AddObservation(sp);
        });
        builder.Services.AddScoped(sp =>
        {
            var factory = sp.GetRequiredService<IDbContextFactory<IntegrationTestDbContext>>();
            return factory.CreateDbContext();
        });

        builder.AddCratisArc(configureOptions: options =>
        {
            options.Hosting.ApplicationUrl = BaseUri.ToString();
            options.GeneratedApis.RoutePrefix = "api";
            options.GeneratedApis.IncludeQueryNameInRoute = true;
            options.GeneratedApis.SegmentsToSkipForRoute = 3;
        });

        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddConsole();
        });

        _app = builder.Build();
        _app.UseCratisArc();

        await _app.StartAsync();

        Services = _app.Services;
        Internals.ServiceProvider = Services;

        EntityChangeTracker = Services.GetRequiredService<IEntityChangeTracker>();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        if (Connection is not null)
        {
            await Connection.CloseAsync();
            await Connection.DisposeAsync();
        }
    }
}
