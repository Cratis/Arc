// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.Json.for_JsonConversion.given;

public class a_json_conversion_context : Specification
{
    protected DbContextOptions<TestDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(":memory:")
            .Options;
    }

    public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<EntityWithJsonProperties> EntitiesWithJson { get; set; } = null!;
        public DbSet<EntityWithoutJsonProperties> EntitiesWithoutJson { get; set; } = null!;
        public DbSet<EntityWithMixedJsonUsage> EntitiesWithMixedJson { get; set; } = null!;
    }
}
