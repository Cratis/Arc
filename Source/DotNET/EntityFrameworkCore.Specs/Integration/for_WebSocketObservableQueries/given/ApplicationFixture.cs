// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    const int Port = 25001;

    public Uri BaseUri { get; private set; }
    public Uri WebSocketBaseUri { get; private set; }
    public IServiceProvider Services { get; private set; }
    public SqliteConnection Connection { get; private set; }
    public IEntityChangeTracker EntityChangeTracker { get; private set; }

    ArcApplication _app;

    public async Task InitializeAsync()
    {
        BaseUri = new Uri($"http://localhost:{Port}/");
        WebSocketBaseUri = new Uri($"ws://localhost:{Port}/");

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
            options.Hosting.ApplicationUrl = $"http://localhost:{Port}/";
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
