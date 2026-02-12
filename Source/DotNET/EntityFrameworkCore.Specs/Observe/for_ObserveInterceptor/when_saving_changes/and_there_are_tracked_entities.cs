// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_ObserveInterceptor.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_ObserveInterceptor.when_saving_changes;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
    public DbSet<TestEntity> TestEntities { get; set; }
}

#pragma warning restore SA1402, SA1649

public class and_there_are_tracked_entities : an_observe_interceptor
{
    TestDbContext _dbContext;

    void Establish()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(_interceptor)
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
    }

    void Because()
    {
        _dbContext.TestEntities.Add(new TestEntity { Id = 1, Name = "Test" });
        _dbContext.SaveChanges();
    }

    [Fact] void should_notify_change_for_tracked_entity_table() => _changeTracker.Received(1).NotifyChange("TestEntities");

    void Cleanup()
    {
        _dbContext?.Database.CloseConnection();
        _dbContext?.Dispose();
    }
}
