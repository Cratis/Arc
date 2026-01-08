// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_ReadOnlyDbContextExtensions;

public class TestReadOnlyDbContext(DbContextOptions<TestReadOnlyDbContext> options) : ReadOnlyDbContext(options)
{
    public DbSet<TestReadOnlyEntity> TestEntities => Set<TestReadOnlyEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new TestReadOnlyEntityConfiguration());
    }
}
