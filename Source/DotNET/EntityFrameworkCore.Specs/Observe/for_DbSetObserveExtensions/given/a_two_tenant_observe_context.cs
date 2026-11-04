// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Arc.Connections;
using Cratis.Arc.EntityFrameworkCore.Connections;
using Cratis.Arc.Queries;
using Cratis.Arc.Tenancy;
using Cratis.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

/// <summary>
/// Base context for observation specs where every tenant has its own SQLite database.
/// </summary>
public class a_two_tenant_observe_context : Specification
{
    protected static readonly TenantId _tenantA = new("tenant-a");
    protected static readonly TenantId _tenantB = new("tenant-b");

    protected IServiceProvider _serviceProvider;
    protected IEntityChangeTracker _entityChangeTracker;
    protected TenantIdAccessor _tenantIdAccessor;
    protected ITenantScope _tenantScope;
    protected ConcurrentQueue<IReadOnlyList<string>> _emissions;
    string _suffix;
    IDisposable _requestFlow;
    SqliteConnection _keepAliveA;
    SqliteConnection _keepAliveB;
    QueryContext _queryContext;

    void Establish()
    {
        _suffix = Guid.NewGuid().ToString("N");
        _emissions = new();
        _queryContext = new QueryContext("[Test]", CorrelationId.New(), Paging.NotPaged, Sorting.None);
        _requestFlow = TenantIdAccessor.Independent.BeginRequest();

        _tenantIdAccessor = new TenantIdAccessor(Substitute.For<ITenantIdResolver>());
        _tenantScope = _tenantIdAccessor;

        var resolver = Substitute.For<ICachingConnectionResolver>();
        resolver.Resolve(Arg.Any<ConnectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new ResolvedConnection(Database: $"observe-{call.Arg<ConnectionRequest>().Scope[WellKnownConnectionScopeKeys.Tenant]}-{_suffix}"));

        var queryContextManager = Substitute.For<IQueryContextManager>();
        queryContextManager.Current.Returns(_ => _queryContext);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEntityFrameworkCoreObservation();
        services.AddSingleton(queryContextManager);
        services.AddSingleton<ITenantIdAccessor>(_tenantIdAccessor);
        services.AddSingleton(_tenantScope);
        services.AddDbContext<TestDbContext>((serviceProvider, options) =>
        {
            var entityChangeTracker = serviceProvider.GetRequiredService<IEntityChangeTracker>();
            var interceptorLogger = serviceProvider.GetRequiredService<ILogger<ObserveInterceptor>>();
            options.UseSqlite("Data Source=unused;Mode=Memory;Cache=Shared")
                   .AddInterceptors(new ObserveInterceptor(entityChangeTracker, interceptorLogger), new ConnectionResolutionInterceptor(resolver, _tenantIdAccessor));
        });

        _serviceProvider = services.BuildServiceProvider();
        Internals.ServiceProvider = _serviceProvider;
        _entityChangeTracker = _serviceProvider.GetRequiredService<IEntityChangeTracker>();

        _keepAliveA = KeepAlive(_tenantA);
        _keepAliveB = KeepAlive(_tenantB);

        Seed(_tenantA, "a-row");
        Seed(_tenantB, "b-secret");
    }

    void Destroy()
    {
        _keepAliveA.Dispose();
        _keepAliveB.Dispose();
        _requestFlow.Dispose();
    }

    /// <summary>
    /// Subscribes to the test entities as a tenant, recording the names of every emission.
    /// </summary>
    /// <param name="tenant">The tenant that subscribes.</param>
    /// <returns>The observable subject.</returns>
    protected ISubject<IEnumerable<TestEntity>> Subscribe(TenantId tenant)
    {
        using var tenantScope = _tenantScope.Begin(tenant);
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        var subject = dbContext.TestEntities.Observe();
        subject.Subscribe(entities => _emissions.Enqueue([.. entities.Select(_ => _.Name)]));
        return subject;
    }

    /// <summary>
    /// Inserts an entity as a tenant, through a fresh scope.
    /// </summary>
    /// <param name="tenant">The tenant that writes.</param>
    /// <param name="name">The name of the entity.</param>
    protected void Insert(TenantId tenant, string name)
    {
        using var tenantScope = _tenantScope.Begin(tenant);
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.TestEntities.Add(new TestEntity { Name = name, IsActive = true });
        dbContext.SaveChanges();
    }

    /// <summary>
    /// Inserts a row straight into a tenant's database, so that no interceptor runs.
    /// </summary>
    /// <param name="tenant">The tenant whose database receives the row.</param>
    /// <param name="name">The name of the entity.</param>
    protected void InsertWithoutNotification(TenantId tenant, string name)
    {
        var connection = tenant == _tenantA ? _keepAliveA : _keepAliveB;
        using var command = connection.CreateCommand();
        command.CommandText = $"INSERT INTO TestEntities (Name, IsActive, SortOrder) VALUES ('{name}', 1, 0)";
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Waits until an emission satisfies a condition.
    /// </summary>
    /// <param name="condition">The condition to wait for.</param>
    protected void WaitForEmission(Func<IReadOnlyList<string>, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (DateTime.UtcNow < deadline && !_emissions.Any(condition))
        {
            Thread.Sleep(10);
        }
    }

    SqliteConnection KeepAlive(TenantId tenant)
    {
        var connection = new SqliteConnection($"Data Source=observe-{tenant}-{_suffix};Mode=Memory;Cache=Shared");
        connection.Open();
        return connection;
    }

    void Seed(TenantId tenant, string name)
    {
        using var tenantScope = _tenantScope.Begin(tenant);
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        dbContext.Database.EnsureCreated();
        dbContext.TestEntities.Add(new TestEntity { Name = name, IsActive = true });
        dbContext.SaveChanges();
    }
}
