// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Cratis.Arc.EntityFrameworkCore.PostgreSql.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA2254 // Template should not vary
#pragma warning disable MA0136 // Raw string contains implicit end of line character

/// <summary>
/// Base context for PostgreSQL observe extension specs.
/// Uses TestContainers to provide a real PostgreSQL instance for testing LISTEN/NOTIFY notifications.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class a_postgresql_observe_context(PostgreSqlFixture fixture) : Specification
{
    protected PostgreSqlFixture _fixture = fixture;
    protected TestDbContext _dbContext;
    protected IQueryContextManager _queryContextManager;
    protected IServiceProvider _serviceProvider;
    protected IEntityChangeTracker _entityChangeTracker;
    protected QueryContext _queryContext;
    protected ILogger _logger;
    protected string _databaseName;

    void Establish()
    {
        // Create a unique database name for this test to avoid conflicts
        _databaseName = $"testdb_{Guid.NewGuid():N}";

        // Create the test database
        CreateTestDatabase();

        // Default query context
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);

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

        // Register DbContext with the PostgreSQL connection
        services.AddDbContext<TestDbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            options.UseNpgsql(GetConnectionString())
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker));
        });

        _serviceProvider = services.BuildServiceProvider();

        // Set the Internals.ServiceProvider so that DbSetObserveExtensions can access it
        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();
        _logger = _serviceProvider.GetRequiredService<ILogger<a_postgresql_observe_context>>();

        // Create database schema
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureCreated();

        // Create the trigger for LISTEN/NOTIFY
        CreateNotifyTrigger();

        // Create the test DbContext for direct use in tests
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(GetConnectionString())
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker))
            .Options;

        _dbContext = new TestDbContext(options);
    }

    void Cleanup()
    {
        _dbContext?.Dispose();

        // Drop the test database
        DropTestDatabase();
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
    /// This bypasses Entity Framework's change tracking and triggers LISTEN/NOTIFY notifications.
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    protected void InsertDirectlyIntoDatabase(params TestEntity[] entities)
    {
        using var connection = new NpgsqlConnection(GetConnectionString());
        connection.Open();

        foreach (var entity in entities)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO \"TestEntities\" (\"Name\", \"IsActive\", \"SortOrder\") VALUES (@Name, @IsActive, @SortOrder)";
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@IsActive", entity.IsActive);
            command.Parameters.AddWithValue("@SortOrder", entity.SortOrder);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Updates an entity directly in the database using raw SQL (simulates external process).
    /// This bypasses Entity Framework's change tracking and triggers LISTEN/NOTIFY notifications.
    /// </summary>
    /// <param name="id">Entity ID to update.</param>
    /// <param name="newName">New name value.</param>
    protected void UpdateDirectlyInDatabase(int id, string newName)
    {
        using var connection = new NpgsqlConnection(GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE \"TestEntities\" SET \"Name\" = @Name WHERE \"Id\" = @Id";
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
        using var connection = new NpgsqlConnection(GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM \"TestEntities\" WHERE \"Id\" = @Id";
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

    string GetConnectionString()
    {
        // Modify the connection string to use our test database
        var builder = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = _databaseName
        };
        return builder.ConnectionString;
    }

    void CreateTestDatabase()
    {
        using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        connection.Open();

        // Create the database
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        createCommand.ExecuteNonQuery();
    }

    void CreateNotifyTrigger()
    {
        using var connection = new NpgsqlConnection(GetConnectionString());
        connection.Open();

        // Create trigger function for LISTEN/NOTIFY
        using var createFunctionCommand = connection.CreateCommand();
        createFunctionCommand.CommandText =
            """
            CREATE OR REPLACE FUNCTION notify_testentities_changes()
            RETURNS trigger AS $$
            BEGIN
                PERFORM pg_notify('testentities_changes', '');
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;
            """;
        createFunctionCommand.ExecuteNonQuery();

        // Create trigger on TestEntities table
        using var createTriggerCommand = connection.CreateCommand();
        createTriggerCommand.CommandText =
            """
            DROP TRIGGER IF EXISTS testentities_notify_trigger ON "TestEntities";
            CREATE TRIGGER testentities_notify_trigger
            AFTER INSERT OR UPDATE OR DELETE ON "TestEntities"
            FOR EACH ROW EXECUTE FUNCTION notify_testentities_changes();
            """;
        createTriggerCommand.ExecuteNonQuery();
    }

    void DropTestDatabase()
    {
        try
        {
            // Force close all connections to the database
            NpgsqlConnection.ClearAllPools();

            using var connection = new NpgsqlConnection(_fixture.ConnectionString);
            connection.Open();

            // Terminate all connections to the database and drop it
            using var terminateCommand = connection.CreateCommand();
            terminateCommand.CommandText =
                $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();
                """;
            terminateCommand.ExecuteNonQuery();

            using var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            dropCommand.ExecuteNonQuery();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
