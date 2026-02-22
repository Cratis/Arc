// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Arc.EntityFrameworkCore.SqlServer.Observe.for_DbSetObserveExtensions.given;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

/// <summary>
/// Test entity for schema_a observe specs.
/// </summary>
public class SchemaEntityA
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Test entity for schema_b observe specs.
/// </summary>
public class SchemaEntityB
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Test DbContext mapping <see cref="SchemaEntityA"/> to schema_a.
/// </summary>
/// <param name="options">The DbContext options.</param>
public class SchemaADbContext(DbContextOptions<SchemaADbContext> options) : DbContext(options)
{
    public DbSet<SchemaEntityA> SchemaEntitiesA { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemaEntityA>().ToTable("SchemaEntitiesA", "schema_a");
    }
}

/// <summary>
/// Test DbContext mapping <see cref="SchemaEntityB"/> to schema_b.
/// </summary>
/// <param name="options">The DbContext options.</param>
public class SchemaBDbContext(DbContextOptions<SchemaBDbContext> options) : DbContext(options)
{
    public DbSet<SchemaEntityB> SchemaEntitiesB { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemaEntityB>().ToTable("SchemaEntitiesB", "schema_b");
    }
}

/// <summary>
/// Combined DbContext used only for creating both schemas and their tables via EnsureCreated.
/// Not registered in DI.
/// </summary>
/// <param name="options">The DbContext options.</param>
internal class SchemaSetupDbContext(DbContextOptions<SchemaSetupDbContext> options) : DbContext(options)
{
    public DbSet<SchemaEntityA> SchemaEntitiesA { get; set; }
    public DbSet<SchemaEntityB> SchemaEntitiesB { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SchemaEntityA>().ToTable("SchemaEntitiesA", "schema_a");
        modelBuilder.Entity<SchemaEntityB>().ToTable("SchemaEntitiesB", "schema_b");
    }
}

#pragma warning restore SA1402, SA1649
