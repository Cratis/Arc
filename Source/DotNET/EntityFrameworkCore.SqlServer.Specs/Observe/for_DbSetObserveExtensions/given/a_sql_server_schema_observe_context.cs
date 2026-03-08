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
/// Base context for SQL Server observe extension specs that use schema-qualified entities.
/// Provides two schemas (schema_a and schema_b) so tests can verify both notification delivery
/// and cross-schema isolation.
/// </summary>
/// <param name="fixture">The SQL Server fixture.</param>
[Collection(SqlServerCollection.Name)]
public class a_sql_server_schema_observe_context(SqlServerFixture fixture) : Specification
{
    protected SqlServerFixture _fixture = fixture;
    protected SchemaADbContext _dbContextA;
    protected SchemaBDbContext _dbContextB;
    protected IQueryContextManager _queryContextManager;
    protected IServiceProvider _serviceProvider;
    protected IEntityChangeTracker _entityChangeTracker;
    protected QueryContext _queryContext;
    protected ILogger _logger;
    protected string _databaseName;

    void Establish()
    {
        _databaseName = $"TestDb_{Guid.NewGuid():N}";
        CreateTestDatabase();

        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Warning)
            .AddConsole());
        services.AddEntityFrameworkCoreObservation();

        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContextManager.Current.Returns(_ => _queryContext);
        services.AddSingleton(_queryContextManager);

        services.AddDbContext<SchemaADbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            var interceptorLogger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
            options.UseSqlServer(GetConnectionString())
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger));
        });

        services.AddDbContext<SchemaBDbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            var interceptorLogger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
            options.UseSqlServer(GetConnectionString())
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger));
        });

        _serviceProvider = services.BuildServiceProvider();
        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();
        _logger = _serviceProvider.GetRequiredService<ILogger<a_sql_server_schema_observe_context>>();

        CreateSchemasAndTables();

        var interceptorLogger = _serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();

        _dbContextA = new SchemaADbContext(new DbContextOptionsBuilder<SchemaADbContext>()
            .UseSqlServer(GetConnectionString())
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker, interceptorLogger))
            .Options);

        _dbContextB = new SchemaBDbContext(new DbContextOptionsBuilder<SchemaBDbContext>()
            .UseSqlServer(GetConnectionString())
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker, interceptorLogger))
            .Options);
    }

    void Cleanup()
    {
        _dbContextA?.Dispose();
        _dbContextB?.Dispose();
        DropTestDatabase();
    }

    /// <summary>
    /// Seeds schema_a with the given entities.
    /// </summary>
    /// <param name="entities">Entities to seed.</param>
    protected void SeedSchemaA(params SchemaEntityA[] entities)
    {
        _dbContextA.SchemaEntitiesA.AddRange(entities);
        _dbContextA.SaveChanges();
    }

    /// <summary>
    /// Seeds schema_b with the given entities.
    /// </summary>
    /// <param name="entities">Entities to seed.</param>
    protected void SeedSchemaB(params SchemaEntityB[] entities)
    {
        _dbContextB.SchemaEntitiesB.AddRange(entities);
        _dbContextB.SaveChanges();
    }

    /// <summary>
    /// Inserts entities directly into schema_a using raw SQL (simulates external process).
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    protected void InsertIntoSchemaA(params SchemaEntityA[] entities)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();

        foreach (var entity in entities)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO [schema_a].[SchemaEntitiesA] ([Name], [IsActive]) VALUES (@Name, @IsActive)";
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@IsActive", entity.IsActive);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Inserts entities directly into schema_b using raw SQL (simulates external process).
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    protected void InsertIntoSchemaB(params SchemaEntityB[] entities)
    {
        using var connection = new SqlConnection(GetConnectionString());
        connection.Open();

        foreach (var entity in entities)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO [schema_b].[SchemaEntitiesB] ([Name], [IsActive]) VALUES (@Name, @IsActive)";
            command.Parameters.AddWithValue("@Name", entity.Name);
            command.Parameters.AddWithValue("@IsActive", entity.IsActive);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Logs information.
    /// </summary>
    /// <param name="message">Message to log.</param>
    protected void LogInfo(string message) => _logger.LogInformation(message);

    string GetConnectionString()
    {
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

        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE [{_databaseName}]";
        createCommand.ExecuteNonQuery();

        using var enableBrokerCommand = connection.CreateCommand();
        enableBrokerCommand.CommandText = $"ALTER DATABASE [{_databaseName}] SET ENABLE_BROKER WITH ROLLBACK IMMEDIATE";
        enableBrokerCommand.ExecuteNonQuery();

        using var dbConnection = new SqlConnection(GetConnectionString());
        dbConnection.Open();

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
    }

    void CreateSchemasAndTables()
    {
        // Use a combined setup DbContext to create both schemas and their tables in one EnsureCreated call
        var setupOptions = new DbContextOptionsBuilder<SchemaSetupDbContext>()
            .UseSqlServer(GetConnectionString())
            .Options;
        using var setupContext = new SchemaSetupDbContext(setupOptions);
        setupContext.Database.EnsureCreated();
    }

    void DropTestDatabase()
    {
        try
        {
            using var connection = new SqlConnection(_fixture.ConnectionString);
            connection.Open();

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
