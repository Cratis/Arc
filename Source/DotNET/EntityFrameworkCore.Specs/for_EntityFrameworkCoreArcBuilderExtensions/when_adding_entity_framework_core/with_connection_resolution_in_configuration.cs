// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Arc.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Arc.EntityFrameworkCore.for_EntityFrameworkCoreArcBuilderExtensions.when_adding_entity_framework_core;

public class with_connection_resolution_in_configuration : Specification
{
    string _directory;
    EntityFrameworkCoreOptions _options;
    string _dataSource;

    void Establish() => _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    async Task Because()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:EntityFrameworkCore:ConnectionString"] = $"Data Source={Path.Combine(_directory, "configured.db")}",
            ["Cratis:EntityFrameworkCore:AutoDiscoverDbContexts"] = "false",
            ["Cratis:EntityFrameworkCore:ConnectionResolution:Connection:Resolver"] = "Template",
            ["Cratis:EntityFrameworkCore:ConnectionResolution:Connection:Template:Database"] = Path.Combine(_directory, "{tenant}.db")
        });

        var tenantIdAccessor = Substitute.For<ITenantIdAccessor>();
        tenantIdAccessor.Current.Returns(new TenantId("acme"));
        builder.Services.AddSingleton(tenantIdAccessor);

        new ArcBuilder(builder, Substitute.For<ITypes>()).WithEntityFrameworkCore(
            configureEfCore: ef =>
            {
                _options = ef.Options;
                ef.AddDbContext<TenantDbContext>();
            });

        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        await dbContext.Database.OpenConnectionAsync();
        _dataSource = ((SqliteConnection)dbContext.Database.GetDbConnection()).DataSource;
        await dbContext.Database.CloseConnectionAsync();
    }

    void Destroy()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }

    [Fact] void should_bind_the_connection_string() => _options.ConnectionString.ShouldContain("configured.db");
    [Fact] void should_bind_the_connection_resolution() => _options.ConnectionResolution.Connection.Resolver.ShouldEqual(ConnectionResolverType.Template);
    [Fact] void should_open_the_tenants_database() => _dataSource.ShouldEqual(Path.Combine(_directory, "acme.db"));

    public class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options);
}
