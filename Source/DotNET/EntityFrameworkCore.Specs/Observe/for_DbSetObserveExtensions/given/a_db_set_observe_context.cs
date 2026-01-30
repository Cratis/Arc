// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA2254 // Template should not vary

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
}

#pragma warning restore SA1402, SA1649

/// <summary>
/// Base context for SQLite observe extension specs.
/// Uses SQLite in-memory database for testing change notifications via update_hook.
/// </summary>
public class a_db_set_observe_context : Specification
{
    protected TestDbContext _dbContext;
    protected IQueryContextManager _queryContextManager;
    protected IServiceProvider _serviceProvider;
    protected IEntityChangeTracker _entityChangeTracker;
    protected QueryContext _queryContext;
    protected ILogger _logger;
    SqliteConnection _connection;

    void Establish()
    {
        // Default query context - tests can override in their own Establish
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);

        // Create a shared SQLite in-memory connection
        // This connection must stay open for the lifetime of the test
        // Otherwise the in-memory database will be destroyed
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        // Set up service collection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddConsole());
        services.AddEntityFrameworkCoreObservation();

        // Use a mocked QueryContextManager that returns the _queryContext field
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContextManager.Current.Returns(_ => _queryContext);
        services.AddSingleton(_queryContextManager);

        // Register DbContext with the shared connection so all scoped instances use the same database
        services.AddDbContext<TestDbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            options.UseSqlite(_connection)
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker));
        });

        _serviceProvider = services.BuildServiceProvider();

        // Set the Internals.ServiceProvider so that DbSetObserveExtensions can access it
        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();
        _logger = _serviceProvider.GetRequiredService<ILogger<a_db_set_observe_context>>();

        // Get the DbContext from DI to ensure it's set up correctly
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureCreated();

        // Create the test DbContext for direct use in tests
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker))
            .Options;

        _dbContext = new TestDbContext(options);
    }

    void Cleanup()
    {
        _dbContext?.Dispose();

        if (_connection is not null)
        {
            _connection.Close();
            _connection.Dispose();
        }
    }

    /// <summary>
    /// Sets the paging for the query context.
    /// </summary>
    /// <param name="paging">The paging settings.</param>
    protected void SetPaging(Paging paging)
    {
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), paging, Sorting.None);
    }

    /// <summary>
    /// Seeds the test database with entities.
    /// </summary>
    /// <param name="entities">Entities to seed.</param>
    protected void SeedTestData(params TestEntity[] entities)
    {
        _dbContext.TestEntities.AddRange(entities);
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Inserts entities directly into the database using raw SQL (simulates external process).
    /// This bypasses Entity Framework's change tracking and triggers SQLite update_hook notifications.
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    protected void InsertDirectlyIntoDatabase(params TestEntity[] entities)
    {
        foreach (var entity in entities)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = "INSERT INTO TestEntities (Name, IsActive, SortOrder) VALUES (@Name, @IsActive, @SortOrder)";
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@IsActive", entity.IsActive ? 1 : 0);
            command.Parameters.AddWithValue("@SortOrder", entity.SortOrder);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Updates an entity directly in the database using raw SQL (simulates external process).
    /// This bypasses Entity Framework's change tracking and triggers SQLite update_hook notifications.
    /// </summary>
    /// <param name="id">Entity ID to update.</param>
    /// <param name="newName">New name value.</param>
    protected void UpdateDirectlyInDatabase(int id, string newName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "UPDATE TestEntities SET Name = @Name WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@Name", newName);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Deletes an entity directly from the database using raw SQL (simulates external process).
    /// </summary>
    /// <param name="id">Entity ID to delete.</param>
    protected void DeleteDirectlyFromDatabase(int id)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM TestEntities WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Logs information.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogInfo(string message)
    {
        _logger.LogInformation(message);
    }
}
