---
title: Configure EF Core entity mappings in Cratis Arc
description: Define EF Core keys, constraints and relationships in entity configuration classes, then use Cratis Arc registration to apply them to your context.
---

Cratis Arc applies EF Core entity mappings from `IEntityTypeConfiguration<T>` classes when you use `BaseDbContext` with Arc context registration. Put keys, constraints and relationships in those classes to keep each entity's mapping separate from the context.

## Overview

Create one configuration class per entity type and expose the entity through a public `DbSet<T>` property on your context. During model creation, [`BaseDbContext`](./base-db-context.md) asks Arc's entity registrar for the matching configuration. You do not need to repeat those mappings in `OnModelCreating`.

## Creating Entity Configurations

Implement EF Core's `IEntityTypeConfiguration<T>` for the entity you want to map. This `User` mapping makes names and email addresses required, gives email a unique index, and relates users to orders:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Configure the primary key
        builder.HasKey(u => u.Id);

        // Configure properties
        builder.Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasMaxLength(255)
            .IsRequired();

        // Configure indexes
        builder.HasIndex(u => u.Email)
            .IsUnique();

        // Configure relationships
        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId);
    }
}
```

## Automatic Discovery and Registration

Register the context through Arc's helpers so EF's options carry the application service provider and entity registrar. The registrar discovers `IEntityTypeConfiguration<T>` implementations, finds the map for each public `DbSet<T>` property, and resolves that map through dependency injection during model creation.

The following context exposes both entities. With Arc registration, the matching configurations are applied without an `OnModelCreating` override. Inheriting from `BaseDbContext` alone does not supply the registrar.

```csharp
public class MyDbContext : BaseDbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }

    // No need to override OnModelCreating - entity maps are applied automatically!
}
```

## Dependency Injection Support

A configuration class can take constructor dependencies. Here, `IConfiguration` supplies the maximum name length instead of fixing it in the mapping:

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private readonly IConfiguration _configuration;

    public UserConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // Use injected configuration
        var maxNameLength = _configuration.GetValue<int>("User:MaxNameLength", 100);
        builder.Property(u => u.Name)
            .HasMaxLength(maxNameLength)
            .IsRequired();
    }
}
```

## Advanced Configuration Examples

### Complex Property Configuration

Use an owned entity when a value such as a shipping address belongs to its order and should be mapped through that relationship. This example also defines a computed total column:

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        // Configure value objects
        builder.OwnsOne(o => o.ShippingAddress, address =>
        {
            address.Property(a => a.Street).HasMaxLength(200);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.PostalCode).HasMaxLength(20);
        });

        // Configure computed columns
        builder.Property(o => o.TotalAmount)
            .HasComputedColumnSql("[Quantity] * [UnitPrice]");
    }
}
```

The computed-column expression uses SQL Server syntax; adapt it for another provider. These examples assume your application's entity declarations.

If you want an object stored as JSON rather than through owned-entity mapping, mark the property with `[Json]` from `Cratis.Arc.EntityFrameworkCore.Json`. `BaseDbContext` discovers it through `ApplyJsonConversion`; there is no Arc `HasJsonConversion()` property extension.

### Multiple Entity Configurations

Keep mappings for different entities in separate classes. The product mapping below sets decimal precision and excludes soft-deleted rows from normal queries; the category mapping defines a parent-child relationship:

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        // Table configuration
        builder.ToTable("Products", "Catalog");

        // Soft delete configuration
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Self-referencing relationship
        builder.HasMany(c => c.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId);
    }
}
```

## Best Practices

### Organization

Use one configuration per entity type: Arc's registrar builds a dictionary keyed by that type, so multiple discovered maps for the same entity are not supported. Naming the class `UserConfiguration` or `OrderConfiguration` makes the matching entity easy to find. Keep it beside the entity or in a mapping folder according to your application's organization.

### Configuration Guidelines

Map the constraints your application depends on. In the `User` example, `IsRequired()` and the length limits define valid column values, while the unique email index prevents duplicate email addresses. Set table and column names explicitly when integrating with an existing schema rather than relying on EF's naming conventions.

### Performance Considerations

Choose indexes from the queries the application actually runs, then inspect those queries against the database. A global query filter such as `!p.IsDeleted` changes which rows normal queries return; it is not an index or a substitute for checking query performance.

## Integration with BaseDbContext

If a configuration is not applied, check that the context exposes the entity through a public `DbSet<T>` and is registered through Arc. The registrar matches those set properties to discovered configurations; it does not apply every configuration to every context.

Keep constructor dependencies suitable for cached EF models. A configuration value such as the maximum name length shapes the model, not an individual request. Do not use request-specific tenant state to shape a pooled context's model.

## Migration Support

Ensure your design-time context creation supplies the same mappings and conversion options as runtime registration. A directly constructed context without the application service provider does not automatically discover maps, so a migration generated from it may not represent the runtime model.

After changing a mapping:

1. Run `dotnet ef migrations add <MigrationName>` to create a migration.
2. Review the generated operations against the intended schema change, especially constraints and provider-specific SQL.
3. Run `dotnet ef database update` against the intended database to apply the reviewed migration.

For changes involving wrapped values or Guid storage, check [concept conversion](./concept-as-conversion.md) and [Guid conversion](./guid-conversion.md) before applying the migration.
