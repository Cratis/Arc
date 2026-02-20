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
/// Base context for PostgreSQL observe extension specs that use schema-qualified entities.
/// Provides two schemas (schema_a and schema_b) so tests can verify both notification delivery
/// and cross-schema isolation using LISTEN/NOTIFY.
/// </summary>
/// <param name="fixture">The PostgreSQL fixture.</param>
[Collection(PostgreSqlCollection.Name)]
public class a_postgresql_schema_observe_context(PostgreSqlFixture fixture) : Specification
{
    protected PostgreSqlFixture _fixture = fixture;
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
        _databaseName = $"testdb_{Guid.NewGuid():N}";
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
            options.UseNpgsql(GetConnectionString())
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger));
        });

        services.AddDbContext<SchemaBDbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            var interceptorLogger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
            options.UseNpgsql(GetConnectionString())
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger));
        });

        _serviceProvider = services.BuildServiceProvider();
        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();
        _logger = _serviceProvider.GetRequiredService<ILogger<a_postgresql_schema_observe_context>>();

        CreateSchemasAndTables();

        var interceptorLogger = _serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();

        _dbContextA = new SchemaADbContext(new DbContextOptionsBuilder<SchemaADbContext>()
            .UseNpgsql(GetConnectionString())
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker, interceptorLogger))
            .Options);

        _dbContextB = new SchemaBDbContext(new DbContextOptionsBuilder<SchemaBDbContext>()
            .UseNpgsql(GetConnectionString())
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
        using var connection = new NpgsqlConnection(GetConnectionString());
        connection.Open();

        foreach (var entity in entities)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO \"schema_a\".\"SchemaEntitiesA\" (\"Name\", \"IsActive\") VALUES (@Name, @IsActive)";
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
        using var connection = new NpgsqlConnection(GetConnectionString());
        connection.Open();

        foreach (var entity in entities)
        {
            using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO \"schema_b\".\"SchemaEntitiesB\" (\"Name\", \"IsActive\") VALUES (@Name, @IsActive)";
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

        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{_databaseName}\"";
        createCommand.ExecuteNonQuery();
    }

    void CreateSchemasAndTables()
    {
        // Use a combined setup DbContext to create both schemas and their tables in one EnsureCreated call
        var setupOptions = new DbContextOptionsBuilder<SchemaSetupDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;
        using var setupContext = new SchemaSetupDbContext(setupOptions);
        setupContext.Database.EnsureCreated();
    }

    void DropTestDatabase()
    {
        try
        {
            NpgsqlConnection.ClearAllPools();

            using var connection = new NpgsqlConnection(_fixture.ConnectionString);
            connection.Open();

            // Escape single quotes in case the database name ever contains them
            var safeDatabaseName = _databaseName.Replace("'", "''");

            using var terminateCommand = connection.CreateCommand();
            terminateCommand.CommandText =
                $"""
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{safeDatabaseName}' AND pid <> pg_backend_pid();
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
