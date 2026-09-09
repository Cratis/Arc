---
title: Entity mapping
description: Discover EF entity configurations through Arc context registration.
---

The Arc provides a clean, organized way to configure your Entity Framework Core entities through the `IEntityTypeConfiguration<T>` interface from Microsoft.EntityFrameworkCore.
This approach separates entity configuration from your DbContext, making your code more maintainable.

## Overview

Entity mapping in the Arc allows you to define how your entities are configured for Entity Framework Core in dedicated classes.
These entity configurations are automatically discovered and applied when your DbContext is created, eliminating the need to override `OnModelCreating` in every DbContext
that inherits from the [`BaseDbContext`](./base-db-context.md).

## Creating Entity Configurations

To create an entity configuration, implement the `IEntityTypeConfiguration<T>` interface for your entity type:

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

Entity configurations are automatically discovered and registered when your application starts. The Arc:

1. **Discovers all implementations** of `IEntityTypeConfiguration<T>` in your application
2. **Registers them with dependency injection** so they can have dependencies injected
3. **Applies them automatically** during `OnModelCreating` in your DbContext

This happens with `BaseDbContext` **and Arc context registration**, which supplies the entity registrar through the options' application service provider. Inheritance alone does not supply that service. Model declaration fragment:

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

Entity configurations support dependency injection, allowing you to inject services that might be needed for configuration:

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

For JSON metadata, mark the entity property with `[Json]` from `Cratis.Arc.EntityFrameworkCore.Json`; `BaseDbContext` discovers it through `ApplyJsonConversion`. There is no Arc `HasJsonConversion()` property extension. The computed-column SQL above is SQL Server-specific; adapt it for other providers. These mapping fragments assume your application's entity declarations.

### Multiple Entity Configurations

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

- **One entity configuration per entity**: Keep each entity configuration focused on a single entity type
- **Descriptive naming**: Use clear names like `UserConfiguration`, `OrderConfiguration`, etc.
- **Logical grouping**: Place entity configurations in a dedicated folder (e.g., `Configuration/` or `Mapping/`)

### Configuration Guidelines

- **Configure all aspects**: Include keys, properties, relationships, and constraints
- **Be explicit**: Don't rely on conventions for critical configurations
- **Use meaningful names**: Configure table and column names explicitly when needed
- **Document complex logic**: Add comments for complex mapping logic

### Performance Considerations

- **Index configuration**: Configure indexes for frequently queried properties
- **Query filters**: Use global query filters for soft delete patterns
- **Relationship loading**: Configure loading behavior for relationships

## Integration with BaseDbContext

When you inherit from `BaseDbContext`, entity configurations are automatically applied during model creation. The base class:

1. Discovers all `DbSet<T>` properties on your DbContext
2. Looks for corresponding `IEntityTypeConfiguration<T>` implementations
3. Applies the configurations during `OnModelCreating`

Arc registration supplies the registrar needed for this discovery. Keep entity configuration dependencies suitable for cached EF models: do not use request-specific tenant state to shape a pooled context's model.

## Migration Support

Entity configurations determine the EF Core model used by migrations. When you change them:

1. Run `dotnet ef migrations add <MigrationName>` to create a migration
2. The migration will include all changes from your entity configurations
3. Run `dotnet ef database update` to apply the changes

Ensure your design-time context creation supplies the same entity mappings and conversion options as runtime registration. A directly constructed context without the application service provider does not automatically discover maps.
