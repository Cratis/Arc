// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Mapping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_concept_as_keys.given;

public class a_test_database : Specification
{
    protected SqliteConnection _connection;
    protected TestDbContextWithConceptKey _context;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var appServices = new ServiceCollection();
        appServices.AddSingleton<IEntityTypeRegistrar, StubEntityTypeRegistrar>();
        var appServiceProvider = appServices.BuildServiceProvider();

        var options = new DbContextOptionsBuilder<TestDbContextWithConceptKey>()
            .UseSqlite(_connection)
            .UseApplicationServiceProvider(appServiceProvider)
            .AddConceptAsSupport()
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        _context = new TestDbContextWithConceptKey(options);
        await _context.Database.EnsureCreatedAsync();
    }

    async Task Cleanup()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    class StubEntityTypeRegistrar : IEntityTypeRegistrar
    {
        public void RegisterEntityMaps(DbContext dbContext, ModelBuilder modelBuilder)
        {
            // Do nothing for specs - we're testing the conversion logic, not entity maps
        }
    }
}
