// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.for_BaseDbContext;

#pragma warning disable SA1402, SA1649 // Single type per file,  File name should match first type name

public class TestDbContext(DbContextOptions<TestDbContext> options) : BaseDbContext(options)
{
    public DbSet<Person> People { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Store> Stores { get; set; }
}

public class EmptyDbContext(DbContextOptions<EmptyDbContext> options) : BaseDbContext(options);

public class DbContextWithOwnedEntities(DbContextOptions<DbContextWithOwnedEntities> options) : BaseDbContext(options)
{
    public DbSet<Person> People { get; set; }
    public DbSet<Product> Products { get; set; }
}

#pragma warning restore SA1402, SA1649 // Single type per file,  File name should match first type name
