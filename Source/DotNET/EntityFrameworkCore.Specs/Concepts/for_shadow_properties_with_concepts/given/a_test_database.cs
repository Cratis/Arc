// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Mapping;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Concepts.for_shadow_properties_with_concepts.given;

public class a_test_database : Specification
{
    protected SqliteConnection _connection;
    protected TestDbContextWithShadowProperties _context;

    async Task Establish()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<TestDbContextWithShadowProperties>()
            .UseSqlite(_connection)
            .UseApplicationServiceProvider(new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .AddSingleton<IEntityTypeRegistrar, StubEntityTypeRegistrar>()
                .BuildServiceProvider())
            .ReplaceService<Microsoft.EntityFrameworkCore.Infrastructure.IModelCustomizer, ConceptAsModelCustomizer>()
            .ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
            .Options;

        _context = new TestDbContextWithShadowProperties(options);
        await _context.Database.EnsureCreatedAsync();
    }

    async Task Destroy()
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
