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

public class SchemaTestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SchemaTestDbContext(DbContextOptions<SchemaTestDbContext> options) : DbContext(options)
{
    public DbSet<SchemaTestEntity> SchemaTestEntities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemaTestEntity>().ToTable("SchemaTestEntities", "testschema");
    }
}

#pragma warning restore SA1402, SA1649

/// <summary>
/// Base context for SQLite observe extension specs with schema-qualified entities.
/// Uses SQLite in-memory database to test schema-qualified key routing via ObserveInterceptor.
/// </summary>
public class a_schema_db_set_observe_context : Specification
{
    protected SchemaTestDbContext _dbContext;
    protected IQueryContextManager _queryContextManager;
    protected IServiceProvider _serviceProvider;
    protected IEntityChangeTracker _entityChangeTracker;
    protected QueryContext _queryContext;
    SqliteConnection _connection;

    void Establish()
    {
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Warning)
            .AddConsole());
        services.AddEntityFrameworkCoreObservation();

        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContextManager.Current.Returns(_ => _queryContext);
        services.AddSingleton(_queryContextManager);

        services.AddSingleton(_connection);

        services.AddDbContext<SchemaTestDbContext>((serviceProvider, options) =>
        {
            var connection = serviceProvider.GetRequiredService<SqliteConnection>();
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            var interceptorLogger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
            options.UseSqlite(connection)
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger));
        });

        _serviceProvider = services.BuildServiceProvider();

        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchemaTestDbContext>();
        dbContext.Database.EnsureCreated();

        var interceptorLogger = _serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
        var options = new DbContextOptionsBuilder<SchemaTestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker, interceptorLogger))
            .Options;

        _dbContext = new SchemaTestDbContext(options);
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
    /// Seeds the test database with entities.
    /// </summary>
    /// <param name="entities">Entities to seed.</param>
    protected void SeedTestData(params SchemaTestEntity[] entities)
    {
        _dbContext.SchemaTestEntities.AddRange(entities);
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// Inserts entities using a separate DbContext instance to trigger change notifications.
    /// </summary>
    /// <param name="entities">Entities to insert.</param>
    protected void InsertDirectlyIntoDatabase(params SchemaTestEntity[] entities)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SchemaTestDbContext>();
        dbContext.SchemaTestEntities.AddRange(entities);
        dbContext.SaveChanges();
    }
}
