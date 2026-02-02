// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe;
using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA2254 // Template should not vary

/// <summary>
/// Base context for SQL Server observe extension specs.
/// Uses TestContainers to provide a real SQL Server instance for testing SqlDependency notifications.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
[Collection(SqlServerCollection.Name)]
public class a_sql_server_observe_context(SqlServerFixture fixture) : Specification
{
    protected SqlServerFixture _fixture = fixture;
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
        _databaseName = $"TestDb_{Guid.NewGuid():N}";

        // Create the test database
        CreateTestDatabase();

        // Default query context
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);

        // Set up service collection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Warning)
            .AddConsole());
        services.AddEntityFrameworkCoreObservation();

        // Use a mocked QueryContextManager that returns the _queryContext field
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContextManager.Current.Returns(_ => _queryContext);
        services.AddSingleton(_queryContextManager);

        // Register DbContext with the SQL Server connection
        services.AddDbContext<TestDbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            var interceptorLogger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
            options.UseSqlServer(GetConnectionString())
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger));
        });

        _serviceProvider = services.BuildServiceProvider();

        // Set the Internals.ServiceProvider so that DbSetObserveExtensions can access it
        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();
        _logger = _serviceProvider.GetRequiredService<ILogger<a_sql_server_observe_context>>();

        // Create database schema
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureCreated();

        // Create the test DbContext for direct use in tests
        var interceptorLogger = _serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(GetConnectionString())
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker, interceptorLogger))
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
    /// This bypasses Entity Framework's change tracking and triggers SqlDependency notifications.
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    protected void InsertDirectlyIntoDatabase(params TestEntity[] entities)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();

        foreach (var entity in entities)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO TestEntities (Name, IsActive, SortOrder) VALUES (@Name, @IsActive, @SortOrder)";
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@IsActive", entity.IsActive);
            command.Parameters.AddWithValue("@SortOrder", entity.SortOrder);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Updates an entity directly in the database using raw SQL (simulates external process).
    /// This bypasses Entity Framework's change tracking and triggers SqlDependency notifications.
    /// </summary>
    /// <param name="id">Entity ID to update.</param>
    /// <param name="newName">New name value.</param>
    protected void UpdateDirectlyInDatabase(int id, string newName)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
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
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();

        using var command = connection.CreateCommand();
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

    string GetConnectionString()
    {
        // Modify the connection string to use our test database
        var builder = new SqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            InitialCatalog = _databaseName
        };
        return builder.ConnectionString;
    }

    void CreateTestDatabase()
    {
        using var connection = new SqlConnection(_fixture.ConnectionString);
        connection.Open();

        // Create the database
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE [{_databaseName}]";
        createCommand.ExecuteNonQuery();

        // Enable Service Broker on the new database
        using var enableBrokerCommand = connection.CreateCommand();
        enableBrokerCommand.CommandText = $"ALTER DATABASE [{_databaseName}] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE";
        enableBrokerCommand.ExecuteNonQuery();

        // Connect to the new database and grant necessary permissions for SqlDependency
        using var dbConnection = new SqlConnection(GetConnectionString());
        dbConnection.Open();

        // Grant SUBSCRIBE QUERY NOTIFICATIONS permission
        using var grantCommand = dbConnection.CreateCommand();
        grantCommand.CommandText = "GRANT SUBSCRIBE QUERY NOTIFICATIONS TO sa";
        try
        {
            grantCommand.ExecuteNonQuery();
        }
        catch
        {
            // Permission might already exist or not needed for sa
        }

        // Verify Service Broker is enabled
        using var verifyCommand = dbConnection.CreateCommand();
        verifyCommand.CommandText = "SELECT is_broker_enabled FROM sys.databases WHERE database_id = DB_ID()";
        var result = verifyCommand.ExecuteScalar();
        Console.WriteLine($"Service Broker enabled after create: {result}");
    }

    void DropTestDatabase()
    {
        try
        {
            using var connection = new SqlConnection(_fixture.ConnectionString);
            connection.Open();

            // Kill all connections to the database first
            using var killCommand = connection.CreateCommand();
            killCommand.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
            killCommand.ExecuteNonQuery();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
