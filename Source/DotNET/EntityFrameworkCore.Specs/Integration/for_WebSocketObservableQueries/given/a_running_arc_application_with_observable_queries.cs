// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.given;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - handled by Cleanup method
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA1849 // Call async methods when in an async method
#pragma warning disable MA0042 // Use async methods

/// <summary>
/// Base context for WebSocket observable query integration specs.
/// Sets up an ArcApplication with Arc.Core, SQLite DbContext, and WebSocket support.
/// </summary>
public class a_running_arc_application_with_observable_queries : Specification
{
    static int _portCounter = 25000;

    protected int _port;
    protected Uri _baseUri;
    protected Uri _webSocketBaseUri;

    protected ArcApplication _app;
    protected IServiceProvider _services;
    protected SqliteConnection _connection;
    protected IntegrationTestDbContext _dbContext;
    protected IEntityChangeTracker _entityChangeTracker;
    protected TestWebSocketClient _webSocketClient;
    protected ILogger _logger;

    async Task Establish()
    {
        // Assign unique port for this test
        _port = Interlocked.Increment(ref _portCounter);
        _baseUri = new Uri($"http://localhost:{_port}/");
        _webSocketBaseUri = new Uri($"ws://localhost:{_port}/");

        // Create a shared SQLite in-memory connection that must stay open for the duration of the test.
        // SQLite in-memory databases are destroyed when the last connection closes.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Build the Arc application
        var builder = ArcApplication.CreateBuilder();

        // Register the shared SQLite connection as singleton so all DbContexts use the same connection
        builder.Services.AddSingleton(_connection);

        // Add Entity Framework Core observation services FIRST (before AddCratisArc)
        // This ensures our singleton registration takes precedence over convention-based registration
        builder.Services.AddEntityFrameworkCoreObservation();

        // Register the DbContext with the shared connection for in-memory SQLite testing.
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

        // Configure Arc options with custom port and route prefix
        builder.AddCratisArc(configureOptions: options =>
        {
            options.Hosting.ApplicationUrl = $"http://localhost:{_port}/";
            options.GeneratedApis.RoutePrefix = "api";
            options.GeneratedApis.IncludeQueryNameInRoute = true;
            options.GeneratedApis.SegmentsToSkipForRoute = 3; // Skip Cratis.Arc.EntityFrameworkCore.Integration
        });

        // Add logging
        builder.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Warning);
            logging.AddConsole();
        });

        // Build and configure the app
        _app = builder.Build();
        _app.UseCratisArc();

        // Start the application
        await _app.StartAsync();

        // Get services from the running application
        _services = _app.Services;
        Internals.ServiceProvider = _services;

        _entityChangeTracker = _services.GetRequiredService<IEntityChangeTracker>();
        _logger = _services.GetRequiredService<ILogger<a_running_arc_application_with_observable_queries>>();

        // Create the database
        using (var scope = _services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
            dbContext.Database.EnsureCreated();
        }

        // Create the test DbContext for direct use
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker))
            .Options;
        _dbContext = new IntegrationTestDbContext(options);

        // Create WebSocket client
        _webSocketClient = new TestWebSocketClient();
    }

    async Task Cleanup()
    {
        await _webSocketClient.Close();
        _webSocketClient.Dispose();

        _dbContext?.Dispose();

        await _app.StopAsync();
        if (_app is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        _connection?.Close();
        _connection?.Dispose();
    }

    /// <summary>
    /// Creates a WebSocket URI for the specified query endpoint.
    /// </summary>
    /// <param name="queryName">The name of the query method.</param>
    /// <returns>The WebSocket URI.</returns>
    protected Uri GetWebSocketUri(string queryName)
    {
        // The namespace is Cratis.Arc.EntityFrameworkCore.Integration
        // After skipping 3 segments (Cratis.Arc.EntityFrameworkCore), we get "Integration"
        // The query name in kebab-case follows
        // Route: /api/integration/{queryName}
        return new Uri(_webSocketBaseUri, $"api/integration/{queryName.ToLowerInvariant()}");
    }

    /// <summary>
    /// Inserts an entity using a scoped DbContext (triggers ObserveInterceptor).
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    protected void InsertEntity(IntegrationTestEntity entity)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        dbContext.Entities.Add(entity);
        dbContext.SaveChanges();
    }

    /// <summary>
    /// Updates an entity using a scoped DbContext (triggers ObserveInterceptor).
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="updateAction">The action to update the entity.</param>
    protected void UpdateEntity(int id, Action<IntegrationTestEntity> updateAction)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        var entity = dbContext.Entities.Find(id);
        if (entity is not null)
        {
            updateAction(entity);
            dbContext.SaveChanges();
        }
    }

    /// <summary>
    /// Deletes an entity using a scoped DbContext (triggers ObserveInterceptor).
    /// </summary>
    /// <param name="id">The entity ID.</param>
    protected void DeleteEntity(int id)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IntegrationTestDbContext>();
        var entity = dbContext.Entities.Find(id);
        if (entity is not null)
        {
            dbContext.Entities.Remove(entity);
            dbContext.SaveChanges();
        }
    }

    /// <summary>
    /// Seeds test data directly (for initial setup, not for testing change detection).
    /// </summary>
    /// <param name="entities">The entities to seed.</param>
    protected void SeedData(params IntegrationTestEntity[] entities)
    {
        _dbContext.Entities.AddRange(entities);
        _dbContext.SaveChanges();
    }
}
