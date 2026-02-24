// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Observe.for_ObserveInterceptor.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Observe.for_ObserveInterceptor.when_saving_changes;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

public class ModelDefaultSchemaEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ModelDefaultSchemaDbContext(DbContextOptions<ModelDefaultSchemaDbContext> options) : DbContext(options)
{
    public DbSet<ModelDefaultSchemaEntity> Entities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("myschema");
    }
}

#pragma warning restore SA1402, SA1649

public class with_model_having_default_schema : an_observe_interceptor
{
    ModelDefaultSchemaDbContext _dbContext;

    void Establish()
    {
        var options = new DbContextOptionsBuilder<ModelDefaultSchemaDbContext>()
            .UseSqlite("Data Source=:memory:")
            .AddInterceptors(_interceptor)
            .Options;

        _dbContext = new ModelDefaultSchemaDbContext(options);
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
    }

    void Because()
    {
        _dbContext.Entities.Add(new ModelDefaultSchemaEntity { Id = 1, Name = "Test" });
        _dbContext.SaveChanges();
    }

    [Fact] void should_notify_change_with_default_schema_qualified_key() => _changeTracker.Received(1).NotifyChange("myschema.Entities");

    void Cleanup()
    {
        _dbContext?.Database.CloseConnection();
        _dbContext?.Dispose();
    }
}
