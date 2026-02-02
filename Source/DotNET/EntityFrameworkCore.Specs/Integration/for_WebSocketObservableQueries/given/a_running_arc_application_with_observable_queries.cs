// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Integration.for_WebSocketObservableQueries.given;

#pragma warning disable CA1001 // Types that own disposable fields should be disposable - handled by Cleanup method
#pragma warning disable CA2000 // Dispose objects before losing scope
#pragma warning disable CA1849 // Call async methods when in an async method
#pragma warning disable MA0042 // Use async methods
#pragma warning disable RCS1141 // Add 'param' element to documentation comment

/// <summary>
/// Base context for WebSocket observable query integration specs.
/// Uses shared ApplicationFixture for the Arc application and creates per-test resources.
/// </summary>
public class a_running_arc_application_with_observable_queries(ApplicationFixture fixture) : Specification
{
    protected IntegrationTestDbContext _dbContext;
    protected TestWebSocketClient _webSocketClient;

    async Task Establish()
    {
        // Clear database before each test to ensure clean state
        ClearDatabase();

        var interceptorLogger = fixture.Services.GetRequiredService<ILogger<ObserveInterceptor>>();
        var options = new DbContextOptionsBuilder<IntegrationTestDbContext>()
            .UseSqlite(fixture.Connection)
            .AddInterceptors(new ObserveInterceptor(fixture.EntityChangeTracker, interceptorLogger))
            .Options;
        _dbContext = new IntegrationTestDbContext(options);

        _webSocketClient = new TestWebSocketClient();

        await Task.CompletedTask;
    }

    async Task Cleanup()
    {
        await _webSocketClient.Close();
        _webSocketClient.Dispose();

        _dbContext?.Dispose();
    }

    /// <summary>
    /// Creates a WebSocket URI for the specified query endpoint.
    /// </summary>
    /// <param name="queryName">The name of the query method.</param>
    /// <returns>The WebSocket URI.</returns>
    protected Uri GetWebSocketUri(string queryName)
    {
        return new Uri(fixture.WebSocketBaseUri, $"api/integration/{queryName.ToLowerInvariant()}");
    }

    /// <summary>
    /// Clears all data from the database between tests.
    /// </summary>
    void ClearDatabase()
    {
        // Use raw SQL to clear the table directly on the shared connection
        using var command = fixture.Connection.CreateCommand();
        command.CommandText = "DELETE FROM Entities";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Inserts an entity using a scoped DbContext (triggers ObserveInterceptor).
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    protected void InsertEntity(IntegrationTestEntity entity)
    {
        using var scope = fixture.Services.CreateScope();
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
        using var scope = fixture.Services.CreateScope();
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
        using var scope = fixture.Services.CreateScope();
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
