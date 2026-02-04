# GUID Conversion

The GUID conversion feature provides automatic handling of `Guid` properties in Entity Framework Core, ensuring consistent storage and optimal database compatibility across different database providers.

## What it does

The GUID conversion automatically configures Entity Framework Core to handle properties of type `Guid` using the most appropriate database representation for each provider:

1. **PostgreSQL**: Stores as native `uuid` type for optimal performance and storage efficiency
2. **SQL Server**: Stores as `uniqueidentifier` type with proper formatting
3. **SQLite**: Stores as `CHAR(36)` with proper string formatting
4. **Other providers**: Uses provider-specific optimizations when available

This automatic configuration ensures that GUIDs are stored in the most efficient format for each database while maintaining compatibility and performance.

## Why it's important

Using GUID conversion provides several key benefits:

- **Cross-Database Compatibility**: Consistent GUID handling across different database providers
- **Performance Optimization**: Uses native GUID types when available for better query performance
- **Storage Efficiency**: Optimizes storage format for each database provider
- **Automatic Configuration**: No need for manual configuration of GUID properties
- **Index Performance**: Ensures GUIDs are stored in formats that support efficient indexing

## Model Usage

Your entity models can use `Guid` properties directly without any special configuration:

```csharp
public class Customer
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Amount { get; set; }
}
```

The conversion will automatically:

- Configure all `Guid` properties to use the optimal storage format for your database provider
- Ensure proper indexing capabilities for GUID-based primary and foreign keys
- Handle conversion between .NET `Guid` instances and database-specific representations

## Manual Configuration

If you're not using the [`BaseDbContext`](./base-db-context.md), you can manually apply GUID conversion in your `DbContext`:

```csharp
using Cratis.Arc.EntityFrameworkCore;

public class StoreDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Customer> Customers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyGuidConversion(database);
        base.OnModelCreating(modelBuilder);
    }
}
```

> Note: This is automatically configured for you when using the [`BaseDbContext`](./base-db-context.md).

## How it works

The conversion system uses reflection to:

1. Identify all properties in your entities that are of type `Guid`
2. Apply the `.AsGuid()` extension method to configure optimal storage for the current database provider
3. Ensure proper value conversion and comparison for change tracking

The conversion is handled by the `GuidConversion.ApplyGuidConversion()` extension method, which automatically discovers and configures all GUID properties in your model.

## Database Provider Specifics

The GUID conversion adapts to different database providers:

- **PostgreSQL (Npgsql)**: Uses native `uuid` type for optimal performance
- **SQL Server**: Uses `uniqueidentifier` with proper collation settings
- **SQLite**: Uses `CHAR(36)` with hyphenated string format
- **MySQL/MariaDB**: Uses `CHAR(36)` with appropriate character set
- **Oracle**: Uses `RAW(16)` for binary storage efficiency

This provider-specific optimization ensures the best performance and storage characteristics for your chosen database.
