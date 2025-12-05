// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

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

public class a_db_set_observe_context : Specification
{
    protected TestDbContext _dbContext;
    protected IQueryContextManager _queryContextManager;
    protected IServiceProvider _serviceProvider;
    protected IEntityChangeTracker _entityChangeTracker;
    protected QueryContext _queryContext;

    void Establish()
    {
        // Default query context - tests can override in their own Establish
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);

        // Set up service collection
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddEntityFrameworkCoreObservation();

        // Use a mocked QueryContextManager that returns the _queryContext field
        _queryContextManager = Substitute.For<IQueryContextManager>();
        _queryContextManager.Current.Returns(_ => _queryContext);
        services.AddSingleton(_queryContextManager);

        _serviceProvider = services.BuildServiceProvider();

        // Set the Internals.ServiceProvider so that DbSetObserveExtensions can access it
        Internals.ServiceProvider = _serviceProvider;

        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();

        // Create in-memory SQLite database
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(new ObserveInterceptor(_entityChangeTracker))
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
    }

    void Cleanup()
    {
        if (_dbContext is not null)
        {
            _dbContext.Database.CloseConnection();
            _dbContext.Dispose();
        }
    }

    protected void SetPaging(Paging paging)
    {
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), paging, Sorting.None);
    }

    protected void SeedTestData(params TestEntity[] entities)
    {
        _dbContext.TestEntities.AddRange(entities);
        _dbContext.SaveChanges();
    }
}
